//go:build integration

package server_test

import (
	"net/http"
	"testing"
	"time"

	"github.com/kazimcavus/pimly/internal/platform/auth"
	"github.com/kazimcavus/pimly/internal/platform/db/dbtest"
	"github.com/kazimcavus/pimly/internal/platform/flags"
	"github.com/kazimcavus/pimly/internal/platform/provision"
	"github.com/kazimcavus/pimly/internal/server"
	"github.com/kazimcavus/pimly/internal/shared/codegen"
)

type batchResp struct {
	Group struct {
		ID        string `json:"id"`
		GroupCode string `json:"group_code"`
		Status    string `json:"status"`
	} `json:"group"`
	Products []struct {
		ID         string `json:"id"`
		ProductSku string `json:"product_sku"`
		Status     string `json:"status"`
		Variants   []struct {
			ID      string `json:"id"`
			Barcode string `json:"barcode"`
		} `json:"variants"`
	} `json:"products"`
}

func setupProductsTenant(t *testing.T) (http.Handler, string) {
	t.Helper()
	database := dbtest.New(t)
	authSvc := auth.NewService(database, "test-secret", time.Hour)
	h := server.New(server.Deps{DB: database, Auth: authSvc, Flags: flags.AlwaysOn{}})
	if _, err := provision.CreateTenant(t.Context(), database, provision.Input{
		Name: "Prod Co", OwnerEmail: "p@x.test", OwnerPassword: "pw",
	}); err != nil {
		t.Fatalf("provision: %v", err)
	}
	token, code := login(t, h, "p@x.test", "pw", "")
	if code != http.StatusOK {
		t.Fatalf("login: %d", code)
	}
	return h, token
}

func TestProductsBatch(t *testing.T) {
	h, token := setupProductsTenant(t)

	body := map[string]any{
		"group": map[string]any{"group_code": "22Y265024", "title": "Basic Tee", "status": "draft"},
		"products": []map[string]any{
			{
				"code": "R01", "title": "Red",
				"variants": []map[string]any{
					{"axis_value": "S", "price": 199.90, "stock": 5},
					{"axis_value": "M", "price": 199.90, "stock": 8},
					{"axis_value": "L", "price": 199.90, "stock": 3},
				},
			},
			{
				"code": "R02", "title": "Blue",
				"variants": []map[string]any{
					{"axis_value": "M", "price": 209.90, "stock": 4},
					{"axis_value": "L", "price": 209.90, "stock": 6},
					{"axis_value": "XL", "price": 209.90, "stock": 2},
					{"axis_value": "XXL", "price": 209.90, "stock": 1},
				},
			},
		},
	}
	rec := request(t, h, "POST", "/products:batch", token, body)
	if rec.Code != http.StatusCreated {
		t.Fatalf("batch: code=%d body=%s", rec.Code, rec.Body)
	}
	var resp batchResp
	mustJSON(t, rec.Body.Bytes(), &resp)

	if resp.Group.GroupCode != "22Y265024" {
		t.Fatalf("group_code = %q", resp.Group.GroupCode)
	}
	if len(resp.Products) != 2 {
		t.Fatalf("products = %d, want 2", len(resp.Products))
	}
	if resp.Products[0].ProductSku != "22Y265024R01" || resp.Products[1].ProductSku != "22Y265024R02" {
		t.Fatalf("skus = %q / %q", resp.Products[0].ProductSku, resp.Products[1].ProductSku)
	}
	// Ragged: 3 vs 4 variants.
	if len(resp.Products[0].Variants) != 3 || len(resp.Products[1].Variants) != 4 {
		t.Fatalf("ragged variant counts = %d / %d, want 3 / 4", len(resp.Products[0].Variants), len(resp.Products[1].Variants))
	}

	// All barcodes auto-generated, valid EAN-13, and unique.
	seen := map[string]bool{}
	for _, p := range resp.Products {
		for _, v := range p.Variants {
			if err := codegen.ValidateEAN13(v.Barcode); err != nil {
				t.Fatalf("invalid barcode %q: %v", v.Barcode, err)
			}
			if seen[v.Barcode] {
				t.Fatalf("duplicate barcode %q", v.Barcode)
			}
			seen[v.Barcode] = true
		}
	}
	if len(seen) != 7 {
		t.Fatalf("unique barcodes = %d, want 7", len(seen))
	}

	// Nested read returns the same tree.
	rec = request(t, h, "GET", "/groups/"+resp.Group.ID, token, nil)
	var nested batchResp
	mustJSON(t, rec.Body.Bytes(), &nested)
	if len(nested.Products) != 2 {
		t.Fatalf("nested products = %d, want 2", len(nested.Products))
	}
}

func TestProductsBatchOverridesAndValidation(t *testing.T) {
	h, token := setupProductsTenant(t)

	overrideBarcode, _ := codegen.GenerateEAN13("123456789012")

	// Manual SKU + barcode override is preserved.
	rec := request(t, h, "POST", "/products:batch", token, map[string]any{
		"group": map[string]any{"group_code": "OVR1"},
		"products": []map[string]any{
			{"product_sku": "CUSTOMSKU1", "variants": []map[string]any{
				{"barcode": overrideBarcode, "price": 10, "stock": 1},
			}},
		},
	})
	if rec.Code != http.StatusCreated {
		t.Fatalf("override batch: code=%d body=%s", rec.Code, rec.Body)
	}
	var resp batchResp
	mustJSON(t, rec.Body.Bytes(), &resp)
	if resp.Products[0].ProductSku != "CUSTOMSKU1" {
		t.Fatalf("override sku = %q", resp.Products[0].ProductSku)
	}
	if resp.Products[0].Variants[0].Barcode != overrideBarcode {
		t.Fatalf("override barcode = %q, want %q", resp.Products[0].Variants[0].Barcode, overrideBarcode)
	}

	// Duplicate group_code → conflict.
	if rec := request(t, h, "POST", "/products:batch", token, map[string]any{
		"group": map[string]any{"group_code": "OVR1"}, "products": []map[string]any{},
	}); rec.Code != http.StatusConflict {
		t.Fatalf("duplicate group_code: code=%d, want 409", rec.Code)
	}

	// --- active enforcement of a required attribute ---
	catID := createID(t, h, token, "/categories", map[string]any{"name": "Cat"})
	attrID := createID(t, h, token, "/attributes", map[string]any{
		"key": "zorunlu_grup", "label": "Zorunlu", "data_type": "text", "binding_level": "group",
	})
	createID(t, h, token, "/categories/"+catID+"/attributes", map[string]any{
		"attribute_id": attrID, "required": true,
	})

	// active + missing required → 400.
	if rec := request(t, h, "POST", "/products:batch", token, map[string]any{
		"group":    map[string]any{"category_id": catID, "status": "active"},
		"products": []map[string]any{},
	}); rec.Code != http.StatusBadRequest {
		t.Fatalf("active missing required: code=%d body=%s, want 400", rec.Code, rec.Body)
	}

	// draft + missing required → ok (lenient).
	if rec := request(t, h, "POST", "/products:batch", token, map[string]any{
		"group":    map[string]any{"category_id": catID, "status": "draft"},
		"products": []map[string]any{},
	}); rec.Code != http.StatusCreated {
		t.Fatalf("draft missing required: code=%d body=%s, want 201", rec.Code, rec.Body)
	}

	// active + provided required → ok.
	if rec := request(t, h, "POST", "/products:batch", token, map[string]any{
		"group": map[string]any{
			"category_id": catID, "status": "active",
			"attribute_values": map[string]any{"zorunlu_grup": "Pamuk"},
		},
		"products": []map[string]any{},
	}); rec.Code != http.StatusCreated {
		t.Fatalf("active with required: code=%d body=%s, want 201", rec.Code, rec.Body)
	}
}
