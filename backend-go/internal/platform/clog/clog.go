// Package clog, Serilog'un CompactJsonFormatter (CLEF) biçimiyle uyumlu JSON
// üreten bir slog.Handler sağlar. Amaç, mevcut monitoring zincirinin
// (Promtail → Loki → Grafana) Go binary'lerinin loglarını .NET API'ninkilerle
// aynı şekilde ayrıştırabilmesidir. Promtail şu alanları bekler:
//
//	@t  — RFC3339Nano zaman damgası (UTC)
//	@l  — seviye adı; CLEF geleneğiyle Information seviyesinde ALAN YAZILMAZ
//	@mt — mesaj şablonu (slog mesajı)
//	@tr — etkin OpenTelemetry trace kimliği (varsa)
//	service, environment — statik özellikler
//	Diğer özellikler PascalCase adlarla üst düzeyde yer alır
//	(ör. RequestMethod, RequestPath, StatusCode, ErrorCode, UserId).
//
// Log çağrılarında öznitelik adları bu sözleşmeye uygun (PascalCase) verilmelidir.
package clog

import (
	"context"
	"encoding/json"
	"log/slog"
	"os"
	"sync"
	"time"

	"go.opentelemetry.io/otel/trace"
)

// Options, handler'ın statik ayarlarını taşır.
type Options struct {
	// Service, her kayda "service" özelliği olarak eklenir (ör. "pimly-api").
	Service string

	// Environment, her kayda "environment" özelliği olarak eklenir
	// (ör. "Development", "Production").
	Environment string

	// Level, yazılacak asgari log seviyesidir.
	Level slog.Level
}

// Handler, CLEF biçiminde JSON üreten slog.Handler uygulamasıdır.
// stdout'a satır satır yazar; Docker log sürücüsü ve Promtail bu satırları toplar.
type Handler struct {
	opts  Options
	attrs []slog.Attr
	mu    *sync.Mutex
	out   *json.Encoder
}

// NewHandler, verilen ayarlarla stdout'a yazan bir CLEF handler'ı oluşturur.
func NewHandler(opts Options) *Handler {
	return &Handler{
		opts: opts,
		mu:   &sync.Mutex{},
		out:  json.NewEncoder(os.Stdout),
	}
}

// SetDefault, verilen ayarlarla CLEF handler'ını süreç genelinde varsayılan
// slog logger'ı yapar ve logger'ı döner. Her binary main() içinde bir kez çağırır.
func SetDefault(opts Options) *slog.Logger {
	logger := slog.New(NewHandler(opts))
	slog.SetDefault(logger)
	return logger
}

// Enabled, seviyenin yazılıp yazılmayacağını döner.
func (h *Handler) Enabled(_ context.Context, level slog.Level) bool {
	return level >= h.opts.Level
}

// levelName, slog seviyesini Serilog seviye adına çevirir. Information özel
// durumdur: CLEF geleneğiyle @l alanı hiç yazılmaz (boş dizgi bunu işaretler).
func levelName(level slog.Level) string {
	switch {
	case level < slog.LevelInfo:
		return "Debug"
	case level < slog.LevelWarn:
		return "" // Information — @l yazılmaz
	case level < slog.LevelError:
		return "Warning"
	default:
		return "Error"
	}
}

// Handle, kaydı CLEF JSON satırı olarak stdout'a yazar.
func (h *Handler) Handle(ctx context.Context, record slog.Record) error {
	event := make(map[string]any, record.NumAttrs()+len(h.attrs)+6)
	event["@t"] = record.Time.UTC().Format(time.RFC3339Nano)
	event["@mt"] = record.Message
	if name := levelName(record.Level); name != "" {
		event["@l"] = name
	}
	if span := trace.SpanContextFromContext(ctx); span.HasTraceID() {
		event["@tr"] = span.TraceID().String()
		event["@sp"] = span.SpanID().String()
	}
	event["service"] = h.opts.Service
	if h.opts.Environment != "" {
		event["environment"] = h.opts.Environment
	}
	for _, attr := range h.attrs {
		putAttr(event, attr)
	}
	record.Attrs(func(attr slog.Attr) bool {
		putAttr(event, attr)
		return true
	})

	h.mu.Lock()
	defer h.mu.Unlock()
	return h.out.Encode(event)
}

// putAttr, slog özniteliğini JSON'a uygun bir değere indirger ve olaya ekler.
func putAttr(event map[string]any, attr slog.Attr) {
	value := attr.Value.Resolve()
	if value.Kind() == slog.KindGroup {
		for _, member := range value.Group() {
			putAttr(event, member)
		}
		return
	}
	event[attr.Key] = value.Any()
}

// WithAttrs, verilen öznitelikleri her kayda ekleyen yeni bir handler döner.
func (h *Handler) WithAttrs(attrs []slog.Attr) slog.Handler {
	clone := *h
	clone.attrs = append(append([]slog.Attr{}, h.attrs...), attrs...)
	return &clone
}

// WithGroup, slog grup desteğidir; CLEF düz alan adları kullandığından gruplar
// düzleştirilir ve grup adı yok sayılır.
func (h *Handler) WithGroup(string) slog.Handler { return h }
