//go:build integration

package server_test

import (
	"bytes"
	"encoding/json"
	"io"
	"net/http"
	"net/http/httptest"
	"testing"
	"time"

	"github.com/kazimcavus/pimly/internal/platform/auth"
	"github.com/kazimcavus/pimly/internal/platform/db/dbtest"
	"github.com/kazimcavus/pimly/internal/platform/flags"
	"github.com/kazimcavus/pimly/internal/server"
)

func TestFeatureFlagsAndAdmin(t *testing.T) {
	database := dbtest.New(t)
	authSvc := auth.NewService(database, "test-secret", time.Hour)
	h := server.New(server.Deps{
		DB:         database,
		Auth:       authSvc,
		Flags:      flags.NewDBChecker(database),
		AdminToken: "admintok",
	})

	// Admin access requires the token.
	if rec := adminReq(t, h, "GET", "/admin/tenants", "", nil); rec.Code != http.StatusForbidden {
		t.Fatalf("admin without token: code=%d, want 403", rec.Code)
	}

	// Create + approve an application → provisions a tenant.
	rec := adminReq(t, h, "POST", "/admin/applications", "admintok", map[string]any{
		"email": "owner@acme.test", "company_name": "Acme Co",
	})
	if rec.Code != http.StatusCreated {
		t.Fatalf("create application: code=%d body=%s", rec.Code, rec.Body)
	}
	var app struct {
		ID string `json:"id"`
	}
	mustJSON(t, rec.Body.Bytes(), &app)

	rec = adminReq(t, h, "POST", "/admin/applications/"+app.ID+"/approve", "admintok", nil)
	if rec.Code != http.StatusOK {
		t.Fatalf("approve: code=%d body=%s", rec.Code, rec.Body)
	}
	var appr struct {
		Tenant struct {
			ID   string `json:"id"`
			Slug string `json:"slug"`
		} `json:"tenant"`
		OwnerEmail        string `json:"owner_email"`
		GeneratedPassword string `json:"generated_password"`
	}
	mustJSON(t, rec.Body.Bytes(), &appr)
	if appr.GeneratedPassword == "" || appr.Tenant.ID == "" {
		t.Fatalf("approve result incomplete: %+v", appr)
	}

	// The provisioned owner can log in (proves provisioning worked).
	token, code := login(t, h, appr.OwnerEmail, appr.GeneratedPassword, "")
	if code != http.StatusOK {
		t.Fatalf("login provisioned owner: code=%d", code)
	}

	// Approving again → conflict.
	if rec := adminReq(t, h, "POST", "/admin/applications/"+app.ID+"/approve", "admintok", nil); rec.Code != http.StatusConflict {
		t.Fatalf("re-approve: code=%d, want 409", rec.Code)
	}

	// --- module flag enforcement ---
	// integration is disabled by default → gated endpoint 403.
	if rec := request(t, h, "GET", "/integration/status", token, nil); rec.Code != http.StatusForbidden {
		t.Fatalf("gated (disabled): code=%d, want 403", rec.Code)
	}

	// Admin enables integration → gated endpoint now 200 (checked live).
	if rec := adminReq(t, h, "POST", "/admin/tenants/"+appr.Tenant.ID+"/modules/integration", "admintok", map[string]any{"enabled": true}); rec.Code != http.StatusOK {
		t.Fatalf("enable module: code=%d body=%s", rec.Code, rec.Body)
	}
	if rec := request(t, h, "GET", "/integration/status", token, nil); rec.Code != http.StatusOK {
		t.Fatalf("gated (enabled): code=%d, want 200", rec.Code)
	}

	// Disable again → 403.
	if rec := adminReq(t, h, "POST", "/admin/tenants/"+appr.Tenant.ID+"/modules/integration", "admintok", map[string]any{"enabled": false}); rec.Code != http.StatusOK {
		t.Fatalf("disable module: code=%d", rec.Code)
	}
	if rec := request(t, h, "GET", "/integration/status", token, nil); rec.Code != http.StatusForbidden {
		t.Fatalf("gated (re-disabled): code=%d, want 403", rec.Code)
	}

	// Tenant appears in the admin listing.
	if n := adminArrLen(t, h, "/admin/tenants"); n < 1 {
		t.Fatalf("admin tenants = %d, want >=1", n)
	}
}

func adminReq(t *testing.T, h http.Handler, method, path, adminToken string, body any) *httptest.ResponseRecorder {
	t.Helper()
	var r io.Reader
	if body != nil {
		b, _ := json.Marshal(body)
		r = bytes.NewReader(b)
	}
	req := httptest.NewRequest(method, path, r)
	if adminToken != "" {
		req.Header.Set("X-Admin-Token", adminToken)
	}
	if body != nil {
		req.Header.Set("Content-Type", "application/json")
	}
	rec := httptest.NewRecorder()
	h.ServeHTTP(rec, req)
	return rec
}

func adminArrLen(t *testing.T, h http.Handler, path string) int {
	t.Helper()
	rec := adminReq(t, h, "GET", path, "admintok", nil)
	if rec.Code != http.StatusOK {
		t.Fatalf("GET %s: code=%d", path, rec.Code)
	}
	var arr []json.RawMessage
	mustJSON(t, rec.Body.Bytes(), &arr)
	return len(arr)
}
