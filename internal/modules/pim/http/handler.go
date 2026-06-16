// Package pimhttp exposes the PIM module's HTTP handlers. Handlers resolve the
// tenant from the request context and run queries in a tenant-scoped
// transaction. Routes are registered via RegisterRoutes.
package pimhttp

import (
	"net/http"

	"github.com/jackc/pgx/v5"

	"github.com/kazimcavus/pimly/internal/platform/db"
	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/tenant"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// Handler holds the PIM module dependencies.
type Handler struct {
	db *db.DB
}

// NewHandler builds a PIM HTTP handler.
func NewHandler(database *db.DB) *Handler { return &Handler{db: database} }

// withTenant runs fn against the request's tenant schema in a transaction.
func (h *Handler) withTenant(r *http.Request, fn func(*tenantdb.Queries) error) error {
	t, ok := tenant.FromContext(r.Context())
	if !ok {
		return apperr.Unauthorized("no tenant in context")
	}
	return h.db.WithTenant(r.Context(), t.SchemaName, func(tx pgx.Tx) error {
		return fn(tenantdb.New(tx))
	})
}
