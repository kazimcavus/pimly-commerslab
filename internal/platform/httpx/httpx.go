// Package httpx holds HTTP primitives shared by all modules: JSON encode/decode
// helpers, a uniform error envelope, and middleware (request id, logging, panic
// recovery).
package httpx

import (
	"context"
	"encoding/json"
	"log/slog"
	"net/http"
	"time"

	"github.com/google/uuid"

	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// JSON writes v as a JSON response with the given status code.
func JSON(w http.ResponseWriter, status int, v any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	if v != nil {
		_ = json.NewEncoder(w).Encode(v)
	}
}

// errorBody is the uniform error envelope.
type errorBody struct {
	Error struct {
		Code    string `json:"code"`
		Message string `json:"message"`
	} `json:"error"`
}

// Error maps err to an HTTP status + JSON envelope. Internal errors are logged
// and their detail is hidden from the client.
func Error(w http.ResponseWriter, r *http.Request, err error) {
	status := apperr.HTTPStatus(err)
	var body errorBody
	body.Error.Code = string(apperr.KindOf(err))
	if status >= 500 {
		body.Error.Message = "internal error"
		slog.ErrorContext(r.Context(), "request error", "err", err, "request_id", RequestIDFromContext(r.Context()))
	} else {
		body.Error.Message = err.Error()
	}
	JSON(w, status, body)
}

// Decode reads a JSON request body into v, rejecting unknown fields.
func Decode(r *http.Request, v any) error {
	dec := json.NewDecoder(r.Body)
	dec.DisallowUnknownFields()
	if err := dec.Decode(v); err != nil {
		return apperr.Validation("invalid request body: %v", err)
	}
	return nil
}

// --- middleware ---

type ctxKey string

const requestIDKey ctxKey = "request_id"

// RequestIDFromContext returns the request id stored in ctx, if any.
func RequestIDFromContext(ctx context.Context) string {
	id, _ := ctx.Value(requestIDKey).(string)
	return id
}

// RequestID assigns a request id (honoring an inbound X-Request-Id) and exposes
// it on the context and response header.
func RequestID(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		id := r.Header.Get("X-Request-Id")
		if id == "" {
			id = uuid.NewString()
		}
		w.Header().Set("X-Request-Id", id)
		ctx := context.WithValue(r.Context(), requestIDKey, id)
		next.ServeHTTP(w, r.WithContext(ctx))
	})
}

type statusRecorder struct {
	http.ResponseWriter
	status int
}

func (s *statusRecorder) WriteHeader(code int) {
	s.status = code
	s.ResponseWriter.WriteHeader(code)
}

// Logger logs one line per request with method, path, status, and duration.
func Logger(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := time.Now()
		rec := &statusRecorder{ResponseWriter: w, status: http.StatusOK}
		next.ServeHTTP(rec, r)
		slog.InfoContext(r.Context(), "http request",
			"method", r.Method,
			"path", r.URL.Path,
			"status", rec.status,
			"duration_ms", time.Since(start).Milliseconds(),
			"request_id", RequestIDFromContext(r.Context()),
		)
	})
}

// Recover converts panics into 500 responses.
func Recover(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		defer func() {
			if rec := recover(); rec != nil {
				slog.ErrorContext(r.Context(), "panic recovered", "panic", rec,
					"request_id", RequestIDFromContext(r.Context()))
				Error(w, r, apperr.E(apperr.KindInternal, "internal error"))
			}
		}()
		next.ServeHTTP(w, r)
	})
}
