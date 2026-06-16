//go:build integration

package server_test

import (
	"bytes"
	"context"
	"encoding/json"
	"io"
	"net/http"
	"net/http/httptest"
	"testing"
	"time"

	"github.com/jackc/pgx/v5"

	"github.com/kazimcavus/pimly/internal/platform/auth"
	"github.com/kazimcavus/pimly/internal/platform/db/dbtest"
	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/flags"
	"github.com/kazimcavus/pimly/internal/platform/provision"
	"github.com/kazimcavus/pimly/internal/server"
)

func TestAuthAndTenantRouting(t *testing.T) {
	ctx := context.Background()
	database := dbtest.New(t)
	authSvc := auth.NewService(database, "test-secret", time.Hour)
	handler := server.New(server.Deps{DB: database, Auth: authSvc, Flags: flags.AlwaysOn{}})

	a, err := provision.CreateTenant(ctx, database, provision.Input{Name: "Acme", OwnerEmail: "a@x.test", OwnerPassword: "pass-a"})
	if err != nil {
		t.Fatalf("provision A: %v", err)
	}
	b, err := provision.CreateTenant(ctx, database, provision.Input{Name: "Beta", OwnerEmail: "b@x.test", OwnerPassword: "pass-b"})
	if err != nil {
		t.Fatalf("provision B: %v", err)
	}

	// --- login returns a token ---
	tokenA, code := login(t, handler, "a@x.test", "pass-a", "")
	if code != http.StatusOK || tokenA == "" {
		t.Fatalf("login A: code=%d token=%q", code, tokenA)
	}

	// --- /me is scoped to the user's tenant ---
	rec := request(t, handler, "GET", "/me", tokenA, nil)
	if rec.Code != http.StatusOK {
		t.Fatalf("GET /me: code=%d body=%s", rec.Code, rec.Body)
	}
	var me struct {
		Tenant struct {
			Slug   string `json:"slug"`
			Schema string `json:"schema"`
		} `json:"tenant"`
	}
	mustJSON(t, rec.Body.Bytes(), &me)
	if me.Tenant.Slug != a.Tenant.Slug {
		t.Fatalf("/me tenant slug = %q, want %q", me.Tenant.Slug, a.Tenant.Slug)
	}

	// --- tenant-scoped read returns A's seed (2 defs) ---
	if n := defCount(t, handler, tokenA); n != 2 {
		t.Fatalf("A definitions = %d, want 2", n)
	}

	// Insert a distinct definition into A only.
	if err := database.WithTenant(ctx, a.Tenant.SchemaName, func(tx pgx.Tx) error {
		_, err := tenantdb.New(tx).CreateMetaobjectDefinition(ctx, tenantdb.CreateMetaobjectDefinitionParams{Key: "marka", Label: "Marka"})
		return err
	}); err != nil {
		t.Fatalf("insert into A: %v", err)
	}
	if n := defCount(t, handler, tokenA); n != 3 {
		t.Fatalf("A definitions after insert = %d, want 3", n)
	}

	// --- another tenant's data is unreachable: B still sees only its 2 ---
	tokenB, code := login(t, handler, "b@x.test", "pass-b", "")
	if code != http.StatusOK {
		t.Fatalf("login B: code=%d", code)
	}
	_ = b
	if n := defCount(t, handler, tokenB); n != 2 {
		t.Fatalf("B definitions = %d, want 2 (isolation breach)", n)
	}

	// --- unauthorized requests are rejected ---
	if rec := request(t, handler, "GET", "/me", "", nil); rec.Code != http.StatusUnauthorized {
		t.Fatalf("GET /me without token: code=%d, want 401", rec.Code)
	}
	if rec := request(t, handler, "GET", "/me", "garbage.token.here", nil); rec.Code != http.StatusUnauthorized {
		t.Fatalf("GET /me with bad token: code=%d, want 401", rec.Code)
	}
	if _, code := login(t, handler, "a@x.test", "wrong", ""); code != http.StatusUnauthorized {
		t.Fatalf("login with wrong password: code=%d, want 401", code)
	}
}

func login(t *testing.T, h http.Handler, email, pass, slug string) (string, int) {
	t.Helper()
	rec := request(t, h, "POST", "/login", "", map[string]string{"email": email, "password": pass, "tenant_slug": slug})
	if rec.Code != http.StatusOK {
		return "", rec.Code
	}
	var resp struct {
		Token string `json:"token"`
	}
	mustJSON(t, rec.Body.Bytes(), &resp)
	return resp.Token, rec.Code
}

func defCount(t *testing.T, h http.Handler, token string) int {
	t.Helper()
	rec := request(t, h, "GET", "/metaobject-definitions", token, nil)
	if rec.Code != http.StatusOK {
		t.Fatalf("GET /metaobject-definitions: code=%d body=%s", rec.Code, rec.Body)
	}
	var defs []map[string]any
	mustJSON(t, rec.Body.Bytes(), &defs)
	return len(defs)
}

func request(t *testing.T, h http.Handler, method, path, token string, body any) *httptest.ResponseRecorder {
	t.Helper()
	var r io.Reader
	if body != nil {
		b, _ := json.Marshal(body)
		r = bytes.NewReader(b)
	}
	req := httptest.NewRequest(method, path, r)
	if token != "" {
		req.Header.Set("Authorization", "Bearer "+token)
	}
	if body != nil {
		req.Header.Set("Content-Type", "application/json")
	}
	rec := httptest.NewRecorder()
	h.ServeHTTP(rec, req)
	return rec
}

func mustJSON(t *testing.T, data []byte, v any) {
	t.Helper()
	if err := json.Unmarshal(data, v); err != nil {
		t.Fatalf("unmarshal %s: %v", data, err)
	}
}
