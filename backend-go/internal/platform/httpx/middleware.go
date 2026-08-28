package httpx

import (
	"context"
	"log/slog"
	"net/http"
	"runtime/debug"
	"time"

	"go.opentelemetry.io/otel/trace"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// traceIDKey ve userIDKey, context'te trace/kullanıcı kimliği taşıyan özel anahtarlardır.
type (
	traceIDKey struct{}
	userIDKey  struct{}
)

// TraceIDFromContext, isteğin trace kimliğini döner; yoksa boş dizgidir.
func TraceIDFromContext(ctx context.Context) string {
	if id, ok := ctx.Value(traceIDKey{}).(string); ok {
		return id
	}
	return ""
}

// UserIDFromContext, kimliği doğrulanmış kullanıcının kimliğini döner;
// anonim isteklerde .NET request loglarıyla aynı "(anonymous)" değeri döner.
func UserIDFromContext(ctx context.Context) string {
	if id, ok := ctx.Value(userIDKey{}).(string); ok && id != "" {
		return id
	}
	return "(anonymous)"
}

// WithUserID, auth middleware'inin doğruladığı kullanıcı kimliğini context'e koyar.
func WithUserID(ctx context.Context, userID string) context.Context {
	return context.WithValue(ctx, userIDKey{}, userID)
}

// TraceID, etkin OpenTelemetry span'inin trace kimliğini isteğe bağlar ve
// X-Trace-Id yanıt başlığını ekler (.NET HttpContextObservability karşılığı).
// OTel etkin değilse kimlik boş kalır; başlık yine de yazılmaz.
func TraceID(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		id := ""
		if span := trace.SpanContextFromContext(r.Context()); span.HasTraceID() {
			id = span.TraceID().String()
		}
		if id != "" {
			w.Header().Set("X-Trace-Id", id)
		}
		next.ServeHTTP(w, r.WithContext(context.WithValue(r.Context(), traceIDKey{}, id)))
	})
}

// Recovery, handler paniklerini yakalayıp 500 ProblemDetails'e çevirir
// (.NET GlobalExceptionHandler'ın bilinmeyen istisna dalının karşılığı).
// Panik ayrıntısı yanıt gövdesine sızdırılmaz, yalnızca loglanır.
func Recovery(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		defer func() {
			if rec := recover(); rec != nil {
				slog.ErrorContext(r.Context(),
					"Unhandled panic while processing {RequestMethod} {RequestPath}.",
					slog.String("RequestMethod", r.Method),
					slog.String("RequestPath", r.URL.Path),
					slog.Any("Panic", rec),
					slog.String("StackTrace", string(debug.Stack())),
				)
				WriteProblem(w, r, sharedkernel.NewInternalError("An unexpected error occurred."))
			}
		}()
		next.ServeHTTP(w, r)
	})
}

// statusRecorder, yanıt durum kodunu loglama için yakalar.
type statusRecorder struct {
	http.ResponseWriter
	status int
}

// WriteHeader, durum kodunu kaydedip sarmalanan yazıcıya iletir.
func (sr *statusRecorder) WriteHeader(code int) {
	sr.status = code
	sr.ResponseWriter.WriteHeader(code)
}

// RequestLogging, her isteği Serilog istek loglarıyla aynı alan adlarıyla loglar.
// excludePrefixes ile eşleşen yollar (sağlık, metrik, medya) loglanmaz;
// seviye durum kodundan türetilir: >=500 Error, >=400 Warning, aksi halde Information.
func RequestLogging(excludePrefixes []string) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			for _, prefix := range excludePrefixes {
				if r.URL.Path == prefix || (len(r.URL.Path) > len(prefix) && r.URL.Path[:len(prefix)+1] == prefix+"/") {
					next.ServeHTTP(w, r)
					return
				}
			}

			start := time.Now()
			rec := &statusRecorder{ResponseWriter: w, status: http.StatusOK}
			next.ServeHTTP(rec, r)

			level := slog.LevelInfo
			switch {
			case rec.status >= http.StatusInternalServerError:
				level = slog.LevelError
			case rec.status >= http.StatusBadRequest:
				level = slog.LevelWarn
			}
			slog.Default().Log(r.Context(), level,
				"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed} ms",
				slog.String("RequestMethod", r.Method),
				slog.String("RequestPath", r.URL.Path),
				slog.Int("StatusCode", rec.status),
				slog.Float64("Elapsed", float64(time.Since(start).Microseconds())/1000.0),
				slog.String("UserId", UserIDFromContext(r.Context())),
			)
		})
	}
}
