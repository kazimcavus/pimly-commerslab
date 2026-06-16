// Package apperr defines a small typed-error taxonomy used across pimly, plus a
// consistent mapping to HTTP status codes. Handlers convert any error into a
// uniform JSON envelope via the httpx package.
package apperr

import (
	stderrors "errors"
	"fmt"
	"net/http"
)

// Kind classifies an error for transport mapping (HTTP status, etc.).
type Kind string

const (
	KindInternal     Kind = "internal"
	KindValidation   Kind = "validation"
	KindNotFound     Kind = "not_found"
	KindConflict     Kind = "conflict"
	KindUnauthorized Kind = "unauthorized"
	KindForbidden    Kind = "forbidden"
)

// Error is the canonical application error. It carries a Kind, a human-readable
// message, and an optional wrapped cause.
type Error struct {
	Kind    Kind
	Message string
	Err     error
}

func (e *Error) Error() string {
	if e.Err != nil {
		return fmt.Sprintf("%s: %s: %v", e.Kind, e.Message, e.Err)
	}
	return fmt.Sprintf("%s: %s", e.Kind, e.Message)
}

func (e *Error) Unwrap() error { return e.Err }

// E builds an Error of the given kind with a formatted message.
func E(kind Kind, format string, a ...any) *Error {
	return &Error{Kind: kind, Message: fmt.Sprintf(format, a...)}
}

// Wrap builds an Error of the given kind wrapping cause.
func Wrap(kind Kind, cause error, format string, a ...any) *Error {
	return &Error{Kind: kind, Message: fmt.Sprintf(format, a...), Err: cause}
}

// Convenience constructors.
func Validation(format string, a ...any) *Error   { return E(KindValidation, format, a...) }
func NotFound(format string, a ...any) *Error     { return E(KindNotFound, format, a...) }
func Conflict(format string, a ...any) *Error     { return E(KindConflict, format, a...) }
func Unauthorized(format string, a ...any) *Error { return E(KindUnauthorized, format, a...) }
func Forbidden(format string, a ...any) *Error    { return E(KindForbidden, format, a...) }
func Internal(cause error) *Error                 { return Wrap(KindInternal, cause, "internal error") }

// KindOf returns the Kind of err, defaulting to KindInternal.
func KindOf(err error) Kind {
	var e *Error
	if stderrors.As(err, &e) {
		return e.Kind
	}
	return KindInternal
}

// HTTPStatus maps an error to an HTTP status code.
func HTTPStatus(err error) int {
	switch KindOf(err) {
	case KindValidation:
		return http.StatusBadRequest
	case KindNotFound:
		return http.StatusNotFound
	case KindConflict:
		return http.StatusConflict
	case KindUnauthorized:
		return http.StatusUnauthorized
	case KindForbidden:
		return http.StatusForbidden
	default:
		return http.StatusInternalServerError
	}
}
