// Package tenant owns tenant identity: schema-name validation/derivation, slug
// generation, and the request-scoped tenant carrier. This package is a leaf
// (no DB dependency) so it can be imported by both the db layer and provisioning
// without creating an import cycle.
package tenant

import (
	"context"
	"regexp"
	"strings"

	"github.com/google/uuid"

	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// schemaNameRe is the multi-tenancy security boundary. A schema name is only
// ever interpolated into SQL (SET LOCAL search_path / CREATE SCHEMA) after
// passing this check — it can never originate from raw user input.
var schemaNameRe = regexp.MustCompile(`^tenant_[a-z0-9_]{1,48}$`)

const schemaPrefix = "tenant_"

// SchemaName derives the canonical Postgres schema name for a tenant slug.
func SchemaName(slug string) string { return schemaPrefix + slug }

// ValidateSchemaName enforces the schema-name pattern and rejects anything else.
func ValidateSchemaName(s string) error {
	if !schemaNameRe.MatchString(s) {
		return apperr.Validation("invalid schema name %q", s)
	}
	return nil
}

// turkishReplacer folds common Turkish characters to ASCII for slugs.
var turkishReplacer = strings.NewReplacer(
	"ş", "s", "Ş", "s",
	"ğ", "g", "Ğ", "g",
	"ı", "i", "İ", "i",
	"ç", "c", "Ç", "c",
	"ö", "o", "Ö", "o",
	"ü", "u", "Ü", "u",
)

// Slugify turns a display name into a slug safe for use as a schema suffix:
// lowercase ASCII letters/digits with single underscores between runs, trimmed,
// capped at 40 chars. Returns "" if nothing usable remains.
func Slugify(name string) string {
	s := turkishReplacer.Replace(strings.TrimSpace(name))
	s = strings.ToLower(s)
	var b strings.Builder
	pendingUnderscore := false
	for _, r := range s {
		switch {
		case (r >= 'a' && r <= 'z') || (r >= '0' && r <= '9'):
			if pendingUnderscore && b.Len() > 0 {
				b.WriteByte('_')
			}
			pendingUnderscore = false
			b.WriteRune(r)
		default:
			pendingUnderscore = true
		}
	}
	out := strings.Trim(b.String(), "_")
	if len(out) > 40 {
		out = strings.Trim(out[:40], "_")
	}
	return out
}

// Tenant is the request-scoped tenant context populated by auth middleware (M2)
// and read by handlers to scope DB access.
type Tenant struct {
	ID         uuid.UUID
	Slug       string
	SchemaName string
	Role       string
}

type ctxKey struct{}

// NewContext returns a copy of ctx carrying t.
func NewContext(ctx context.Context, t Tenant) context.Context {
	return context.WithValue(ctx, ctxKey{}, t)
}

// FromContext extracts the tenant carried by ctx, if any.
func FromContext(ctx context.Context) (Tenant, bool) {
	t, ok := ctx.Value(ctxKey{}).(Tenant)
	return t, ok
}
