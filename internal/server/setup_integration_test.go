//go:build integration

package server_test

import (
	"encoding/json"
	"net/http"
	"testing"
	"time"

	"github.com/kazimcavus/pimly/internal/platform/auth"
	"github.com/kazimcavus/pimly/internal/platform/db/dbtest"
	"github.com/kazimcavus/pimly/internal/platform/flags"
	"github.com/kazimcavus/pimly/internal/platform/provision"
	"github.com/kazimcavus/pimly/internal/server"
)

func TestSetupDefinitions(t *testing.T) {
	database := dbtest.New(t)
	authSvc := auth.NewService(database, "test-secret", time.Hour)
	h := server.New(server.Deps{DB: database, Auth: authSvc, Flags: flags.AlwaysOn{}})

	if _, err := provision.CreateTenant(t.Context(), database, provision.Input{
		Name: "Setup Co", OwnerEmail: "s@x.test", OwnerPassword: "pw",
	}); err != nil {
		t.Fatalf("provision: %v", err)
	}
	token, code := login(t, h, "s@x.test", "pw", "")
	if code != http.StatusOK {
		t.Fatalf("login: %d", code)
	}

	// Category "T-Shirt".
	catID := createID(t, h, token, "/categories", map[string]any{"name": "T-Shirt", "code": "TS"})

	// Attributes: kumas (text) and yaka_tipi (single_select inline).
	kumasID := createID(t, h, token, "/attributes", map[string]any{
		"key": "kumas", "label": "Kumaş", "data_type": "text",
	})
	yakaID := createID(t, h, token, "/attributes", map[string]any{
		"key": "yaka_tipi", "label": "Yaka Tipi", "data_type": "single_select",
		"value_source": "inline", "inline_options": []string{"Bisiklet Yaka", "V Yaka"},
	})

	// Use the seeded "renk" metaobject definition; add Kırmızı{hex} and Beyaz.
	renkID := findDefinitionID(t, h, token, "renk")
	createID(t, h, token, "/metaobject-definitions/"+renkID+"/entries", map[string]any{
		"values": map[string]any{"ad": "Kırmızı", "hex": "#FF0000"},
	})
	createID(t, h, token, "/metaobject-definitions/"+renkID+"/entries", map[string]any{
		"values": map[string]any{"ad": "Beyaz"},
	})
	if n := arrLen(t, h, token, "/metaobject-definitions/"+renkID+"/entries"); n != 2 {
		t.Fatalf("renk entries = %d, want 2", n)
	}

	// Assign attributes to the category with required flags.
	createID(t, h, token, "/categories/"+catID+"/attributes", map[string]any{
		"attribute_id": kumasID, "required": true, "sort_order": 1,
	})
	createID(t, h, token, "/categories/"+catID+"/attributes", map[string]any{
		"attribute_id": yakaID, "required": false, "marketplace_required": true, "sort_order": 2,
	})
	if n := arrLen(t, h, token, "/categories/"+catID+"/attributes"); n != 2 {
		t.Fatalf("category attributes = %d, want 2", n)
	}

	// --- validation failures ---
	if rec := request(t, h, "POST", "/attributes", token, map[string]any{
		"key": "bad", "label": "Bad", "data_type": "metaobject_ref", "value_source": "none",
	}); rec.Code != http.StatusBadRequest {
		t.Fatalf("invalid attribute consistency: code=%d, want 400", rec.Code)
	}
	if rec := request(t, h, "POST", "/metaobject-definitions/"+renkID+"/entries", token, map[string]any{
		"values": map[string]any{"unknown_field": "x"},
	}); rec.Code != http.StatusBadRequest {
		t.Fatalf("unknown entry field: code=%d, want 400", rec.Code)
	}
}

func createID(t *testing.T, h http.Handler, token, path string, body any) string {
	t.Helper()
	rec := request(t, h, "POST", path, token, body)
	if rec.Code != http.StatusCreated {
		t.Fatalf("POST %s: code=%d body=%s", path, rec.Code, rec.Body)
	}
	var resp struct {
		ID string `json:"id"`
	}
	mustJSON(t, rec.Body.Bytes(), &resp)
	if resp.ID == "" {
		t.Fatalf("POST %s: empty id", path)
	}
	return resp.ID
}

func findDefinitionID(t *testing.T, h http.Handler, token, key string) string {
	t.Helper()
	rec := request(t, h, "GET", "/metaobject-definitions", token, nil)
	var defs []struct {
		ID  string `json:"id"`
		Key string `json:"key"`
	}
	mustJSON(t, rec.Body.Bytes(), &defs)
	for _, d := range defs {
		if d.Key == key {
			return d.ID
		}
	}
	t.Fatalf("definition %q not found", key)
	return ""
}

func arrLen(t *testing.T, h http.Handler, token, path string) int {
	t.Helper()
	rec := request(t, h, "GET", path, token, nil)
	if rec.Code != http.StatusOK {
		t.Fatalf("GET %s: code=%d", path, rec.Code)
	}
	var arr []json.RawMessage
	mustJSON(t, rec.Body.Bytes(), &arr)
	return len(arr)
}
