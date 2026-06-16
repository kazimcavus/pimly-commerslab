package flags

import (
	"context"
	"errors"
	"net/http"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"

	"github.com/kazimcavus/pimly/internal/platform/db"
	"github.com/kazimcavus/pimly/internal/platform/db/globaldb"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/platform/tenant"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

// DBChecker reads module enablement from public.tenant_modules. The core PIM
// module is always enabled.
type DBChecker struct {
	db *db.DB
}

// NewDBChecker builds a DB-backed flag checker.
func NewDBChecker(database *db.DB) *DBChecker { return &DBChecker{db: database} }

func (c *DBChecker) Enabled(ctx context.Context, tenantID uuid.UUID, module Module) (bool, error) {
	if module == ModulePIM {
		return true, nil
	}
	m, err := globaldb.New(c.db.Pool()).GetTenantModule(ctx, globaldb.GetTenantModuleParams{
		TenantID: tenantID,
		Module:   string(module),
	})
	if errors.Is(err, pgx.ErrNoRows) {
		return false, nil
	}
	if err != nil {
		return false, err
	}
	return m.Enabled, nil
}

// RequireModule returns middleware that allows the request only if the given
// module is enabled for the request's tenant.
func RequireModule(c Checker, module Module) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			t, ok := tenant.FromContext(r.Context())
			if !ok {
				httpx.Error(w, r, apperr.Unauthorized("not authenticated"))
				return
			}
			enabled, err := c.Enabled(r.Context(), t.ID, module)
			if err != nil {
				httpx.Error(w, r, apperr.Internal(err))
				return
			}
			if !enabled {
				httpx.Error(w, r, apperr.Forbidden("module %q is not enabled for this tenant", module))
				return
			}
			next.ServeHTTP(w, r)
		})
	}
}
