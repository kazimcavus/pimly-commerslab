// Package httpx, HTTP katmanının ortak yapı taşlarını içerir: RFC 7807
// ProblemDetails üretimi, JSON okuma/yazma yardımcıları, sayfalama sorgu
// çözümü ve middleware zinciri (trace kimliği, panik kurtarma, istek loglama).
// .NET tarafındaki Pimly.AspNetCore projesinin karşılığıdır; ürettiği yanıt
// gövdeleri parite testleriyle .NET çıktısına karşı doğrulanır.
package httpx

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"strings"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// StatusForErrorCode, üst düzey hata kodunu HTTP durum koduna eşler
// (.NET ProblemDetailsFactory.MapStatusCode karşılığı).
func StatusForErrorCode(code string) int {
	switch code {
	case sharedkernel.ErrorCodeValidation:
		return http.StatusBadRequest
	case sharedkernel.ErrorCodeNotFound:
		return http.StatusNotFound
	case sharedkernel.ErrorCodeConflict:
		return http.StatusConflict
	case sharedkernel.ErrorCodeUnauthorized:
		return http.StatusUnauthorized
	case sharedkernel.ErrorCodeInternal:
		return http.StatusInternalServerError
	default:
		return http.StatusBadRequest
	}
}

// fieldError, errors sözlüğündeki tek bir alan hatasının kablo biçimidir.
type fieldError struct {
	Code    string `json:"code"`
	Message string `json:"message"`
}

// problemBody, RFC 7807 gövdesinin kablo biçimidir ve .NET çıktısıyla birebir
// aynıdır (parite golden'larıyla doğrulanmıştır): "type" alanı YOKTUR ve içerik
// türü application/json'dır — .NET'in LoggingProblemResult'ı böyle üretir.
// errors yalnızca doğrulama hatalarında bulunur.
type problemBody struct {
	Title   string                  `json:"title"`
	Status  int                     `json:"status"`
	Detail  string                  `json:"detail"`
	TraceID string                  `json:"trace_id"`
	Errors  map[string][]fieldError `json:"errors,omitempty"`
}

// WriteProblem, domain hatasını ProblemDetails yanıtı olarak yazar ve
// istemci hatalarını (4xx) Warning, sunucu hatalarını (5xx) Error seviyesinde
// loglar (.NET ApiFailureLogger karşılığı). traceID, TraceIDMiddleware'in
// isteğe eklediği kimliktir.
func WriteProblem(w http.ResponseWriter, r *http.Request, derr *sharedkernel.Error) {
	status := StatusForErrorCode(derr.Code)
	body := problemBody{
		Title:   derr.Code,
		Status:  status,
		Detail:  derr.Message,
		TraceID: TraceIDFromContext(r.Context()),
	}
	if len(derr.ValidationErrors) > 0 {
		body.Errors = make(map[string][]fieldError)
		for _, ve := range derr.ValidationErrors {
			body.Errors[ve.Field] = append(body.Errors[ve.Field], fieldError{Code: ve.Code, Message: ve.Message})
		}
	}

	logFailure(r, status, derr)
	writeJSON(w, status, "application/json; charset=utf-8", body)
}

// logFailure, başarısız isteği Promtail'in beklediği alan adlarıyla loglar.
func logFailure(r *http.Request, status int, derr *sharedkernel.Error) {
	level := slog.LevelWarn
	if status >= http.StatusInternalServerError {
		level = slog.LevelError
	}
	attrs := []any{
		slog.String("RequestMethod", r.Method),
		slog.String("RequestPath", r.URL.Path),
		slog.Int("StatusCode", status),
		slog.String("ErrorCode", derr.Code),
		slog.String("UserId", UserIDFromContext(r.Context())),
	}
	if len(derr.ValidationErrors) > 0 {
		fields := make([]string, 0, len(derr.ValidationErrors))
		for _, ve := range derr.ValidationErrors {
			fields = append(fields, ve.Field)
		}
		attrs = append(attrs, slog.String("ValidationFields", strings.Join(fields, ",")))
	}
	slog.Default().Log(r.Context(), level,
		"Request {RequestMethod} {RequestPath} failed with {StatusCode}.", attrs...)
}

// writeJSON, gövdeyi verilen içerik türüyle serileştirip yazar.
func writeJSON(w http.ResponseWriter, status int, contentType string, body any) {
	w.Header().Set("Content-Type", contentType)
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(body)
}
