// Package pimhttp exposes the PIM module's HTTP handlers. It is intentionally
// thin: handlers resolve the tenant from the request context and run queries in
// a tenant-scoped transaction. M2 ships read-only definition listing; M3+ adds
// full CRUD and the products:batch write path.
package pimhttp

import (
	"net/http"

	"github.com/jackc/pgx/v5"

	"github.com/kazimcavus/pimly/internal/platform/db"
	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
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

// ListMetaobjectDefinitions returns the tenant's metaobject definitions.
func (h *Handler) ListMetaobjectDefinitions(w http.ResponseWriter, r *http.Request) {
	var defs []tenantdb.MetaobjectDefinition
	if err := h.withTenant(r, func(q *tenantdb.Queries) error {
		d, err := q.ListMetaobjectDefinitions(r.Context())
		if err != nil {
			return apperr.Internal(err)
		}
		defs = d
		return nil
	}); err != nil {
		httpx.Error(w, r, err)
		return
	}
	httpx.JSON(w, http.StatusOK, defs)
}
