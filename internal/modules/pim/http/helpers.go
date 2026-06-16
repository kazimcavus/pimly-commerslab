package pimhttp

import (
	"encoding/json"
	"errors"
	"net/http"
	"strconv"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgconn"
	"github.com/jackc/pgx/v5/pgtype"

	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// inTenant runs fn in a tenant-scoped transaction and returns its result.
func inTenant[T any](h *Handler, r *http.Request, fn func(*tenantdb.Queries) (T, error)) (T, error) {
	var out T
	err := h.withTenant(r, func(q *tenantdb.Queries) error {
		v, err := fn(q)
		if err != nil {
			return err
		}
		out = v
		return nil
	})
	return out, err
}

// pathUUID parses a UUID path value.
func pathUUID(r *http.Request, name string) (uuid.UUID, error) {
	id, err := uuid.Parse(r.PathValue(name))
	if err != nil {
		return uuid.Nil, apperr.Validation("invalid %s id", name)
	}
	return id, nil
}

// optUUID parses an optional UUID string from a request field.
func optUUID(s *string, field string) (*uuid.UUID, error) {
	if s == nil || *s == "" {
		return nil, nil
	}
	id, err := uuid.Parse(*s)
	if err != nil {
		return nil, apperr.Validation("invalid %s", field)
	}
	return &id, nil
}

// textPtr converts an optional string to a nullable pgtype.Text.
func textPtr(s *string) pgtype.Text {
	if s == nil {
		return pgtype.Text{}
	}
	return pgtype.Text{String: *s, Valid: true}
}

// textOrNull converts a string to a nullable pgtype.Text (empty → NULL).
func textOrNull(s string) pgtype.Text {
	if s == "" {
		return pgtype.Text{}
	}
	return pgtype.Text{String: s, Valid: true}
}

// normalizeJSON returns a non-null JSONB value for an attribute_values payload.
func normalizeJSON(raw json.RawMessage) json.RawMessage {
	if len(raw) == 0 || string(raw) == "null" {
		return json.RawMessage("{}")
	}
	return raw
}

// numeric converts a float to a pgtype.Numeric.
func numeric(f float64) (pgtype.Numeric, error) {
	var n pgtype.Numeric
	if err := n.Scan(strconv.FormatFloat(f, 'f', -1, 64)); err != nil {
		return n, apperr.Validation("invalid numeric value %v", f)
	}
	return n, nil
}

// nullableNumeric converts an optional float to a nullable pgtype.Numeric.
func nullableNumeric(f *float64) (pgtype.Numeric, error) {
	if f == nil {
		return pgtype.Numeric{}, nil
	}
	return numeric(*f)
}

// dbErr maps storage/query errors to the typed taxonomy. Existing apperr values
// pass through unchanged.
func dbErr(err error) error {
	if err == nil {
		return nil
	}
	var ae *apperr.Error
	if errors.As(err, &ae) {
		return err
	}
	if errors.Is(err, pgx.ErrNoRows) {
		return apperr.NotFound("not found")
	}
	var pgErr *pgconn.PgError
	if errors.As(err, &pgErr) {
		switch pgErr.Code {
		case "23505": // unique_violation
			return apperr.Conflict("already exists")
		case "23503": // foreign_key_violation
			return apperr.Validation("referenced entity does not exist")
		case "23514": // check_violation
			return apperr.Validation("value violates a constraint")
		}
	}
	return apperr.Internal(err)
}
