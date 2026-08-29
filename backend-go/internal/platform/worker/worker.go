// Package worker, worker binary'lerinin ortak yaşam döngüsü iskeletini içerir:
// sinyalle iptal edilen poll döngüsü, /healthz + /metrics sunucusu ve CLEF log
// kurulumu. .NET BackgroundService + Host.CreateApplicationBuilder desenlerinin
// Go karşılığıdır; .NET'ten farklı olarak her worker metrik ve sağlık ucu kazanır.
package worker

import (
	"context"
	"errors"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"pimly.commerslab/backend-go/internal/platform/clog"
	"pimly.commerslab/backend-go/internal/platform/obs"
)

// Setup, worker süreci için ortak başlangıcı yapar: süreç UTC'ye sabitlenir,
// CLEF logger kurulur ve sinyalle iptal edilen kök context döner.
func Setup(serviceName string) (context.Context, context.CancelFunc) {
	// pgx timestamptz değerlerini yerel saate çevirir; yerel = UTC olduğunda
	// zaman damgaları her katmanda UTC kalır.
	time.Local = time.UTC

	environment := os.Getenv("PIMLY_ENVIRONMENT")
	if environment == "" {
		environment = "Development"
	}
	clog.SetDefault(clog.Options{Service: serviceName, Environment: environment, Level: slog.LevelInfo})

	return signal.NotifyContext(context.Background(), syscall.SIGINT, syscall.SIGTERM)
}

// ServeMetrics, /healthz /ready /metrics uçlarını arka planda sunar; addr boşsa
// sunucu açılmaz. Dönen işlev kapanışta çağrılır.
func ServeMetrics(addr string, health *obs.Health) func(context.Context) error {
	if addr == "" {
		return func(context.Context) error { return nil }
	}
	mux := http.NewServeMux()
	mux.Handle("/healthz", health.LivenessHandler())
	mux.Handle("/ready", health.ReadinessHandler())
	mux.Handle("/metrics", obs.MetricsHandler())

	server := &http.Server{Addr: addr, Handler: mux, ReadHeaderTimeout: 10 * time.Second}
	go func() {
		slog.Info("Worker metrics listening.", slog.String("Addr", addr))
		if err := server.ListenAndServe(); !errors.Is(err, http.ErrServerClosed) {
			slog.Error("Worker metrics server failed.", slog.Any("Error", err))
		}
	}()
	return server.Shutdown
}

// RunLoop, iterate işlevini poll aralığıyla çalıştırır (.NET BackgroundService
// döngüsünün karşılığı): iterate true dönerse (iş yapıldı) beklemeden devam
// edilir, false dönerse aralık kadar beklenir; hata loglanır ve döngü sürer.
// Context iptalinde eldeki iterasyon bitirilir ve dönülür.
func RunLoop(ctx context.Context, name string, pollInterval time.Duration, iterate func(context.Context) (bool, error)) {
	slog.Info("Worker loop started.", slog.String("Worker", name),
		slog.Float64("PollIntervalSeconds", pollInterval.Seconds()))
	for {
		if ctx.Err() != nil {
			slog.Info("Worker loop stopped.", slog.String("Worker", name))
			return
		}
		processed, err := iterate(ctx)
		if err != nil {
			if ctx.Err() != nil {
				return
			}
			slog.Error("Worker iteration failed.", slog.String("Worker", name), slog.Any("Error", err))
		}
		if processed {
			continue
		}
		select {
		case <-time.After(pollInterval):
		case <-ctx.Done():
			slog.Info("Worker loop stopped.", slog.String("Worker", name))
			return
		}
	}
}
