// Package server wires the HTTP router: middleware chain plus public and
// authenticated routes. Modules contribute their own handlers.
package server

import (
	"net/http"

	pimhttp "github.com/kazimcavus/pimly/internal/modules/pim/http"
	"github.com/kazimcavus/pimly/internal/platform/auth"
	"github.com/kazimcavus/pimly/internal/platform/db"
	"github.com/kazimcavus/pimly/internal/platform/flags"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/platform/storage"
	"github.com/kazimcavus/pimly/internal/platform/tenant"
)

// Deps are the dependencies required to build the router.
type Deps struct {
	DB      *db.DB
	Auth    *auth.Service
	Flags   flags.Checker
	Storage *storage.Client // may be nil if media storage is not configured
}

// New builds the top-level HTTP handler.
func New(deps Deps) http.Handler {
	mux := http.NewServeMux()

	// Public routes.
	mux.HandleFunc("GET /healthz", func(w http.ResponseWriter, _ *http.Request) {
		_, _ = w.Write([]byte("ok"))
	})
	mux.HandleFunc("GET /readyz", func(w http.ResponseWriter, r *http.Request) {
		if err := deps.DB.Ping(r.Context()); err != nil {
			http.Error(w, "db unavailable", http.StatusServiceUnavailable)
			return
		}
		_, _ = w.Write([]byte("ready"))
	})
	mux.HandleFunc("POST /login", loginHandler(deps.Auth))

	// Authenticated routes (tenant resolved from the bearer token).
	authed := deps.Auth.Authenticate
	mux.Handle("GET /me", authed(http.HandlerFunc(meHandler)))

	pimH := pimhttp.NewHandler(deps.DB, deps.Storage)
	pimH.RegisterRoutes(mux, authed)

	// Global middleware (outermost first).
	return httpx.Recover(httpx.RequestID(httpx.Logger(mux)))
}

func loginHandler(a *auth.Service) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var req struct {
			Email      string `json:"email"`
			Password   string `json:"password"`
			TenantSlug string `json:"tenant_slug"`
		}
		if err := httpx.Decode(r, &req); err != nil {
			httpx.Error(w, r, err)
			return
		}
		res, err := a.Login(r.Context(), req.Email, req.Password, req.TenantSlug)
		if err != nil {
			httpx.Error(w, r, err)
			return
		}
		httpx.JSON(w, http.StatusOK, map[string]any{
			"token":      res.Token,
			"expires_at": res.ExpiresAt,
			"tenant": map[string]any{
				"id":   res.Tenant.ID,
				"slug": res.Tenant.Slug,
				"role": res.Tenant.Role,
			},
		})
	}
}

func meHandler(w http.ResponseWriter, r *http.Request) {
	uid, _ := auth.UserID(r.Context())
	t, _ := tenant.FromContext(r.Context())
	httpx.JSON(w, http.StatusOK, map[string]any{
		"user_id": uid,
		"tenant": map[string]any{
			"id":     t.ID,
			"slug":   t.Slug,
			"schema": t.SchemaName,
			"role":   t.Role,
		},
	})
}
