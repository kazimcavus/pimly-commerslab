// Package obs, gözlemlenebilirlik uçlarını ve OpenTelemetry kurulumunu içerir:
// /healthz (canlılık), /ready (hazır olma), /metrics (Prometheus) ve OTLP gRPC
// üzerinden Tempo'ya trace ihracı. .NET tarafındaki
// Pimly.AspNetCore/Observability'nin karşılığıdır; .NET'ten farklı olarak
// worker binary'leri de bu paketi kullanarak ilk kez metrik ve sağlık ucu kazanır.
package obs

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"sync/atomic"
	"time"

	"github.com/prometheus/client_golang/prometheus/promhttp"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracegrpc"
	"go.opentelemetry.io/otel/propagation"
	"go.opentelemetry.io/otel/sdk/resource"
	sdktrace "go.opentelemetry.io/otel/sdk/trace"
	semconv "go.opentelemetry.io/otel/semconv/v1.26.0"

	"pimly.commerslab/backend-go/internal/platform/config"
)

// ReadyCheck, /ready ucunun çalıştırdığı tek bir hazır olma denetimidir
// (ör. veritabanı ping'i, medya depolama yazma denemesi).
type ReadyCheck struct {
	// Name, denetimin yanıt gövdesindeki adıdır (ör. "catalog-db").
	Name string

	// Check, denetimi çalıştırır; nil dönerse sağlıklıdır.
	Check func(ctx context.Context) error
}

// Health, sağlık uçlarının durumunu yönetir. draining bayrağı graceful
// shutdown sırasında /ready'yi 503'e düşürerek yük dengeleyicinin trafiği
// kesmesini sağlar.
type Health struct {
	checks   []ReadyCheck
	draining atomic.Bool
}

// NewHealth, verilen denetimlerle sağlık yöneticisi oluşturur.
func NewHealth(checks ...ReadyCheck) *Health {
	return &Health{checks: checks}
}

// StartDraining, /ready ucunu kalıcı olarak 503'e düşürür; kapanış sırasında çağrılır.
func (h *Health) StartDraining() { h.draining.Store(true) }

// LivenessHandler, /healthz ucudur: süreç ayaktaysa her zaman 200 {"status":"ok"} döner.
func (h *Health) LivenessHandler() http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, _ *http.Request) {
		writeHealth(w, http.StatusOK, "ok", nil)
	})
}

// ReadinessHandler, /ready ucudur: tüm denetimler geçerse 200, aksi halde
// (veya drenaj başladıysa) 503 döner; denetim sonuçları gövdede listelenir.
func (h *Health) ReadinessHandler() http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if h.draining.Load() {
			writeHealth(w, http.StatusServiceUnavailable, "draining", nil)
			return
		}
		ctx, cancel := context.WithTimeout(r.Context(), 5*time.Second)
		defer cancel()

		results := make(map[string]string, len(h.checks))
		healthy := true
		for _, check := range h.checks {
			if err := check.Check(ctx); err != nil {
				results[check.Name] = err.Error()
				healthy = false
			} else {
				results[check.Name] = "ok"
			}
		}
		status, text := http.StatusOK, "ok"
		if !healthy {
			status, text = http.StatusServiceUnavailable, "unhealthy"
		}
		writeHealth(w, status, text, results)
	})
}

// writeHealth, sağlık yanıt gövdesini yazar.
func writeHealth(w http.ResponseWriter, status int, text string, checks map[string]string) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(status)
	body := map[string]any{"status": text}
	if len(checks) > 0 {
		body["checks"] = checks
	}
	_ = json.NewEncoder(w).Encode(body)
}

// MetricsHandler, Prometheus kazıma ucudur (/metrics).
func MetricsHandler() http.Handler { return promhttp.Handler() }

// SetupTracing, OTLP gRPC ihracatçısıyla küresel OpenTelemetry tracer
// sağlayıcısını kurar ve kapanışta çağrılacak temizleme işlevini döner.
// İzleme kapalıysa işlevsiz bir temizleyici döner; ihracatçıya ulaşılamaması
// başlangıcı engellemez (arka planda yeniden dener).
func SetupTracing(ctx context.Context, cfg config.ObservabilityConfig) (func(context.Context) error, error) {
	if !cfg.Enabled || !cfg.Tracing.Enabled {
		return func(context.Context) error { return nil }, nil
	}

	exporter, err := otlptracegrpc.New(ctx,
		otlptracegrpc.WithEndpointURL(cfg.Tracing.OtlpEndpoint),
		otlptracegrpc.WithInsecure(),
	)
	if err != nil {
		return nil, fmt.Errorf("obs: OTLP ihracatçısı kurulamadı: %w", err)
	}

	res, err := resource.Merge(resource.Default(), resource.NewWithAttributes(
		semconv.SchemaURL,
		semconv.ServiceName(cfg.ServiceName),
		semconv.ServiceVersion(cfg.ServiceVersion),
	))
	if err != nil {
		return nil, fmt.Errorf("obs: OTel kaynağı kurulamadı: %w", err)
	}

	provider := sdktrace.NewTracerProvider(
		sdktrace.WithBatcher(exporter),
		sdktrace.WithResource(res),
		sdktrace.WithSampler(sdktrace.ParentBased(sdktrace.TraceIDRatioBased(cfg.Tracing.SamplingRatio))),
	)
	otel.SetTracerProvider(provider)
	otel.SetTextMapPropagator(propagation.NewCompositeTextMapPropagator(
		propagation.TraceContext{}, propagation.Baggage{}))
	return provider.Shutdown, nil
}
