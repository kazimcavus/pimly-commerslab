package parity

import (
	"encoding/json"
	"fmt"
	"net/http"
	"testing"
	"time"
)

// TestPricingInventoryParity, Pricing ve Inventory uçlarının kablo formatı
// paritesini uçtan uca doğrular. Ondalık hassasiyeti kritik senaryodur:
// 449.90 hiçbir yanıtta 449.9'a çökmemelidir (bu yüzden tutarlar MASKELENMEZ).
func TestPricingInventoryParity(t *testing.T) {
	r := NewRunnerFromEnv("goldens")
	if r == nil {
		t.Skip("PARITY_BASE_URL tanımlı değil; parite testi atlandı")
	}
	if err := r.Login("owner@acme.test", "demo1234"); err != nil {
		t.Fatalf("koşucu girişi: %v", err)
	}

	ts := time.Now().UnixNano()
	run := func(name string, c Case) *Snapshot {
		t.Helper()
		snap, err := r.RunWithResult(c)
		if err != nil {
			t.Errorf("%s: %v", name, err)
		}
		return &snap
	}

	// Hazırlık: kategori + basit ürün (kalem kimliği fiyat/stok uçları için gerekli).
	catSnap, _ := r.send(Case{
		Method: http.MethodPost, Path: "/api/v1/catalog/categories",
		Body: map[string]any{"name": fmt.Sprintf("zzz-parity-price-%d", ts)}, Auth: true,
	})
	var category struct {
		ID string `json:"id"`
	}
	_ = json.Unmarshal(catSnap.Body, &category)

	prodSnap, err := r.send(Case{
		Method: http.MethodPost, Path: "/api/v1/catalog/products:batch",
		Body: map[string]any{
			"group_id": "22222222-3333-4444-5555-666666666666",
			"products": []any{map[string]any{
				"category_id": category.ID, "model_code": fmt.Sprintf("ZZZPRC-%d", ts),
				"name": "Parity Fiyat Ürünü", "status": "draft",
				"items": []any{map[string]any{"barcode": fmt.Sprintf("888%d", ts%1_000_000_000_000)}},
			}},
		}, Auth: true,
	})
	if err != nil || prodSnap.Status != http.StatusCreated {
		t.Fatalf("hazırlık ürünü oluşturulamadı: %v %s", err, prodSnap.Body)
	}
	var batch struct {
		Products []struct {
			Items []struct {
				ID string `json:"id"`
			} `json:"items"`
		} `json:"products"`
	}
	_ = json.Unmarshal(prodSnap.Body, &batch)
	itemID := batch.Products[0].Items[0].ID

	defMasks := map[string]string{"id": MaskUUID, "name": MaskAnyString}

	// --- Fiyat tanımları ---
	defName := fmt.Sprintf("Zzz Parity Fiyat %d", ts)
	defSnap := run("pricing_def_create", Case{
		Name: "pricing/definitions_create", Method: http.MethodPost,
		Path: "/api/v1/pricing/price-definitions",
		Body: map[string]any{"name": defName, "code": "prt_sale"},
		Auth: true, Masks: defMasks,
	})
	var definition struct {
		ID string `json:"id"`
	}
	_ = json.Unmarshal(defSnap.Body, &definition)

	run("pricing_def_conflict", Case{
		Name: "pricing/definitions_conflict", Method: http.MethodPost,
		Path: "/api/v1/pricing/price-definitions",
		Body: map[string]any{"name": defName},
		Auth: true, Masks: problemMasks,
	})
	run("pricing_def_validation", Case{
		Name: "pricing/definitions_validation", Method: http.MethodPost,
		Path: "/api/v1/pricing/price-definitions",
		Body: map[string]any{"name": ""},
		Auth: true, Masks: problemMasks,
	})
	run("pricing_def_update", Case{
		Name: "pricing/definitions_update", Method: http.MethodPatch,
		Path: "/api/v1/pricing/price-definitions/" + definition.ID,
		Body: map[string]any{"name": defName + " v2", "code": nil},
		Auth: true, Masks: defMasks,
	})

	// --- Temel fiyat: önce 404-kayıt-yok, sonra ondalık hassasiyet akışı ---
	run("pricing_base_get_none", Case{
		Name: "pricing/base_price_get_none", Method: http.MethodGet,
		Path: "/api/v1/pricing/items/" + itemID + "/base-price",
		Auth: true, Masks: problemMasks,
	})
	run("pricing_base_put", Case{
		Name: "pricing/base_price_put", Method: http.MethodPut,
		Path: "/api/v1/pricing/items/" + itemID + "/base-price",
		Body: json.RawMessage(`{"amount": 449.90, "compare_at_amount": 599.90}`),
		Auth: true, Masks: map[string]string{"product_item_id": MaskUUID, "updated_at": MaskDateTime},
	})
	run("pricing_base_get", Case{
		Name: "pricing/base_price_get", Method: http.MethodGet,
		Path: "/api/v1/pricing/items/" + itemID + "/base-price",
		Auth: true, Masks: map[string]string{"product_item_id": MaskUUID, "updated_at": MaskDateTime},
	})
	run("pricing_base_negative", Case{
		Name: "pricing/base_price_negative", Method: http.MethodPut,
		Path: "/api/v1/pricing/items/" + itemID + "/base-price",
		Body: json.RawMessage(`{"amount": -1}`),
		Auth: true, Masks: problemMasks,
	})

	// --- Kalem fiyatları ---
	run("pricing_item_put", Case{
		Name: "pricing/item_price_put", Method: http.MethodPut,
		Path: "/api/v1/pricing/items/" + itemID + "/prices/" + definition.ID,
		Body: json.RawMessage(`{"amount": 123.40}`),
		Auth: true, Masks: map[string]string{
			"id": MaskUUID, "product_item_id": MaskUUID, "price_definition_id": MaskUUID,
			"definition_name": MaskAnyString, "updated_at": MaskDateTime,
		},
	})
	run("pricing_item_list", Case{
		Name: "pricing/item_price_list", Method: http.MethodGet,
		Path: "/api/v1/pricing/items/" + itemID + "/prices",
		Auth: true, Masks: map[string]string{
			"*.id": MaskUUID, "*.product_item_id": MaskUUID, "*.price_definition_id": MaskUUID,
			"*.definition_name": MaskAnyString, "*.updated_at": MaskDateTime,
		},
	})
	run("pricing_item_put_unknown_def", Case{
		Name: "pricing/item_price_unknown_def", Method: http.MethodPut,
		Path: "/api/v1/pricing/items/" + itemID + "/prices/00000000-0000-0000-0000-000000000009",
		Body: json.RawMessage(`{"amount": 5}`),
		Auth: true, Masks: problemMasks,
	})
	run("pricing_item_delete", Case{
		Name: "pricing/item_price_delete", Method: http.MethodDelete,
		Path: "/api/v1/pricing/items/" + itemID + "/prices/" + definition.ID, Auth: true,
	})

	// --- Kanal fiyatları ---
	channelMasks := map[string]string{"product_item_id": MaskUUID, "updated_at": MaskDateTime}
	run("pricing_channel_put", Case{
		Name: "pricing/channel_price_put", Method: http.MethodPut,
		Path: "/api/v1/pricing/items/" + itemID + "/channel-prices/TY",
		Body: json.RawMessage(`{"amount": 459.90, "compare_at_amount": 649.00}`),
		Auth: true, Masks: channelMasks,
	})
	run("pricing_channel_get", Case{
		Name: "pricing/channel_price_get", Method: http.MethodGet,
		Path: "/api/v1/pricing/items/" + itemID + "/channel-prices/TY",
		Auth: true, Masks: channelMasks,
	})
	run("pricing_channel_list", Case{
		Name: "pricing/channel_price_list", Method: http.MethodGet,
		Path: "/api/v1/pricing/items/" + itemID + "/channel-prices",
		Auth: true, Masks: map[string]string{"*.product_item_id": MaskUUID, "*.updated_at": MaskDateTime},
	})
	run("pricing_channel_unknown_marketplace", Case{
		Name: "pricing/channel_price_unknown_marketplace", Method: http.MethodGet,
		Path: "/api/v1/pricing/items/" + itemID + "/channel-prices/XX",
		Auth: true, Masks: problemMasks,
	})

	// --- Stok ---
	stockMasks := map[string]string{"product_item_id": MaskUUID, "updated_at": MaskDateTime}
	run("inventory_get_none", Case{
		Name: "inventory/stock_get_none", Method: http.MethodGet,
		Path: "/api/v1/inventory/items/" + itemID + "/stock",
		Auth: true, Masks: problemMasks,
	})
	run("inventory_put", Case{
		Name: "inventory/stock_put", Method: http.MethodPut,
		Path: "/api/v1/inventory/items/" + itemID + "/stock",
		Body: map[string]any{"quantity": 25},
		Auth: true, Masks: stockMasks,
	})
	run("inventory_get", Case{
		Name: "inventory/stock_get", Method: http.MethodGet,
		Path: "/api/v1/inventory/items/" + itemID + "/stock",
		Auth: true, Masks: stockMasks,
	})
	run("inventory_put_negative", Case{
		Name: "inventory/stock_put_negative", Method: http.MethodPut,
		Path: "/api/v1/inventory/items/" + itemID + "/stock",
		Body: map[string]any{"quantity": -5},
		Auth: true, Masks: problemMasks,
	})
	run("inventory_put_unknown_item", Case{
		Name: "inventory/stock_put_unknown_item", Method: http.MethodPut,
		Path: "/api/v1/inventory/items/00000000-0000-0000-0000-000000000009/stock",
		Body: map[string]any{"quantity": 1},
		Auth: true, Masks: problemMasks,
	})

	// Temizlik: tanım ve ürün silinir (golden'a girmez).
	_, _ = r.send(Case{Method: http.MethodDelete,
		Path: "/api/v1/pricing/price-definitions/" + definition.ID, Auth: true})
}
