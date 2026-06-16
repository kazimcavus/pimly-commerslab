// Package admin provides platform-administration endpoints: reviewing and
// approving signup applications (approval provisions a tenant), listing tenants,
// and toggling per-tenant module flags. Admin endpoints are guarded by a static
// admin token (X-Admin-Token) rather than the tenant auth used elsewhere.
package admin

import (
	"errors"
	"net/http"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgtype"

	"github.com/kazimcavus/pimly/internal/platform/db"
	"github.com/kazimcavus/pimly/internal/platform/db/globaldb"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/platform/provision"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

var validModules = map[string]bool{"pim": true, "integration": true, "wms": true}

// Handler holds admin dependencies.
type Handler struct {
	db         *db.DB
	adminToken string
}

// NewHandler builds an admin handler. If adminToken is empty, all admin
// endpoints are denied.
func NewHandler(database *db.DB, adminToken string) *Handler {
	return &Handler{db: database, adminToken: adminToken}
}

// RegisterRoutes mounts admin routes, each guarded by the admin token.
func (h *Handler) RegisterRoutes(mux *http.ServeMux) {
	guard := h.requireAdmin
	mux.Handle("GET /admin/applications", guard(http.HandlerFunc(h.ListApplications)))
	mux.Handle("POST /admin/applications", guard(http.HandlerFunc(h.CreateApplication)))
	mux.Handle("POST /admin/applications/{id}/approve", guard(http.HandlerFunc(h.ApproveApplication)))
	mux.Handle("GET /admin/tenants", guard(http.HandlerFunc(h.ListTenants)))
	mux.Handle("POST /admin/tenants/{id}/modules/{module}", guard(http.HandlerFunc(h.SetModule)))
}

func (h *Handler) requireAdmin(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if h.adminToken == "" || r.Header.Get("X-Admin-Token") != h.adminToken {
			httpx.Error(w, r, apperr.Forbidden("admin access denied"))
			return
		}
		next.ServeHTTP(w, r)
	})
}

func (h *Handler) gq() *globaldb.Queries { return globaldb.New(h.db.Pool()) }

func (h *Handler) ListApplications(w http.ResponseWriter, r *http.Request) {
	var (
		apps []globaldb.Application
		err  error
	)
	if status := r.URL.Query().Get("status"); status != "" {
		apps, err = h.gq().ListApplicationsByStatus(r.Context(), status)
	} else {
		apps, err = h.gq().ListApplications(r.Context())
	}
	if err != nil {
		httpx.Error(w, r, apperr.Internal(err))
		return
	}
	httpx.JSON(w, http.StatusOK, apps)
}

func (h *Handler) CreateApplication(w http.ResponseWriter, r *http.Request) {
	var req struct {
		Email       string `json:"email"`
		CompanyName string `json:"company_name"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	if req.Email == "" || req.CompanyName == "" {
		httpx.Error(w, r, apperr.Validation("email and company_name are required"))
		return
	}
	app, err := h.gq().CreateApplication(r.Context(), globaldb.CreateApplicationParams{
		Email: req.Email, CompanyName: req.CompanyName, Status: "pending",
	})
	if err != nil {
		httpx.Error(w, r, apperr.Internal(err))
		return
	}
	httpx.JSON(w, http.StatusCreated, app)
}

// ApproveApplication marks an application approved and provisions a tenant.
func (h *Handler) ApproveApplication(w http.ResponseWriter, r *http.Request) {
	id, err := parseUUID(r.PathValue("id"))
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	app, err := h.gq().GetApplicationByID(r.Context(), id)
	if errors.Is(err, pgx.ErrNoRows) {
		httpx.Error(w, r, apperr.NotFound("application not found"))
		return
	} else if err != nil {
		httpx.Error(w, r, apperr.Internal(err))
		return
	}
	if app.Status == "approved" {
		httpx.Error(w, r, apperr.Conflict("application already approved"))
		return
	}

	res, err := provision.CreateTenant(r.Context(), h.db, provision.Input{
		Name:       app.CompanyName,
		OwnerEmail: app.Email,
	})
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	if _, err := h.gq().SetApplicationStatus(r.Context(), globaldb.SetApplicationStatusParams{
		ID: id, Status: "approved", ApprovedBy: nil,
	}); err != nil {
		httpx.Error(w, r, apperr.Internal(err))
		return
	}

	httpx.JSON(w, http.StatusOK, map[string]any{
		"tenant": map[string]any{
			"id":     res.Tenant.ID,
			"slug":   res.Tenant.Slug,
			"schema": res.Tenant.SchemaName,
		},
		"owner_email":        res.Owner.Email,
		"generated_password": res.GeneratedPassword,
	})
}

func (h *Handler) ListTenants(w http.ResponseWriter, r *http.Request) {
	tenants, err := h.gq().ListTenants(r.Context())
	if err != nil {
		httpx.Error(w, r, apperr.Internal(err))
		return
	}
	httpx.JSON(w, http.StatusOK, tenants)
}

// SetModule enables or disables a module for a tenant.
func (h *Handler) SetModule(w http.ResponseWriter, r *http.Request) {
	tenantID, err := parseUUID(r.PathValue("id"))
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	module := r.PathValue("module")
	if !validModules[module] {
		httpx.Error(w, r, apperr.Validation("invalid module %q", module))
		return
	}
	var req struct {
		Enabled bool `json:"enabled"`
	}
	if err := httpx.Decode(r, &req); err != nil {
		httpx.Error(w, r, err)
		return
	}
	enabledAt := pgtype.Timestamptz{}
	if req.Enabled {
		enabledAt = pgtype.Timestamptz{Time: time.Now(), Valid: true}
	}
	m, err := h.gq().UpsertTenantModule(r.Context(), globaldb.UpsertTenantModuleParams{
		TenantID: tenantID, Module: module, Enabled: req.Enabled, EnabledAt: enabledAt,
	})
	if err != nil {
		httpx.Error(w, r, apperr.Internal(err))
		return
	}
	httpx.JSON(w, http.StatusOK, m)
}

func parseUUID(s string) (uuid.UUID, error) {
	id, err := uuid.Parse(s)
	if err != nil {
		return uuid.Nil, apperr.Validation("invalid id")
	}
	return id, nil
}
