package parity

import (
	"encoding/json"
	"fmt"
	"net/http"
	"testing"
	"time"
)

// TestChannelsParity, Channels uçlarının kablo formatı paritesini doğrular:
// pazaryeri listesi/bağlantısı, taksonomi durumu ve senkron kuyruğu, harici
// kategori araması, kategori/özellik/değer eşlemeleri, import ve yayın kuyruğu,
// ürün hazırlık raporu. Bağlantı bilgileri iki backend'de aynı DB'den okunduğu
// için maskeler dar tutulmuştur.
func TestChannelsParity(t *testing.T) {
	r := NewRunnerFromEnv("goldens")
	if r == nil {
		t.Skip("PARITY_BASE_URL tanımlı değil; parite testi atlandı")
	}
	if err := r.Login("owner@acme.test", "demo1234"); err != nil {
		t.Fatalf("koşucu girişi: %v", err)
	}

	run := func(name string, c Case) *Snapshot {
		t.Helper()
		snap, err := r.RunWithResult(c)
		if err != nil {
			t.Errorf("%s: %v", name, err)
		}
		return &snap
	}

	// 1) Pazaryerleri ve bağlantı.
	run("marketplaces_list", Case{
		Name: "channels/marketplaces_list", Method: http.MethodGet,
		Path: "/api/v1/channels/marketplaces", Auth: true,
	})
	run("connection_get", Case{
		Name: "channels/connection_get", Method: http.MethodGet,
		Path: "/api/v1/channels/marketplaces/TY/connection", Auth: true,
		Masks: map[string]string{"id": MaskUUID, "seller_id": MaskNullable, "api_key_hint": MaskNullable},
	})
	run("connection_unknown_marketplace", Case{
		Name: "channels/connection_unknown_marketplace", Method: http.MethodGet,
		Path: "/api/v1/channels/marketplaces/XX/connection", Auth: true, Masks: problemMasks,
	})

	// 2) Taksonomi durumu ve kuyruk çakışması (aktif iş varsa 409, yoksa 202;
	//    her iki backend aynı DB'yi gördüğü için sonuç aynıdır).
	run("taxonomy_status", Case{
		Name: "channels/taxonomy_status", Method: http.MethodGet,
		Path: "/api/v1/channels/marketplaces/TY/taxonomy/status", Auth: true,
		Masks: map[string]string{
			"active_sync_run_id": MaskNullable, "last_completed_at": MaskNullable,
			"cached_category_count":                 MaskAnyNumber,
			"last_completed_run.id":                 MaskUUID,
			"last_completed_run.created_at":         MaskDateTime,
			"last_completed_run.started_at":         MaskDateTime,
			"last_completed_run.completed_at":       MaskDateTime,
			"last_completed_run.processed_count":    MaskAnyNumber,
			"last_completed_run.total_estimate":     MaskAnyNumber,
			"last_completed_run.error_message":      MaskNullable,
		},
	})
	run("taxonomy_run_unknown", Case{
		Name: "channels/taxonomy_run_unknown", Method: http.MethodGet,
		Path: "/api/v1/channels/marketplaces/TY/taxonomy/sync-runs/00000000-0000-0000-0000-000000000009",
		Auth: true, Masks: problemMasks,
	})

	// 3) Harici kategori araması (cache'ten okunur, iki backend aynı veriyi görür).
	run("categories_search", Case{
		Name: "channels/categories_search", Method: http.MethodGet,
		Path: "/api/v1/channels/marketplaces/TY/categories?q=hal%C4%B1&limit=3", Auth: true,
		Masks: map[string]string{"*.id": MaskUUID, "*.synced_at": MaskDateTime},
	})

	// 4) Kategori eşlemesi akışı: yeni kategori oluştur → yaprak olmayan/bilinmeyen
	//    harici kategoriyle hata → gerçek yaprakla eşle → getir → listele → sil.
	ts := time.Now().UnixNano()
	catSnap, err := r.send(Case{
		Method: http.MethodPost, Path: "/api/v1/catalog/categories",
		Body: map[string]any{"name": fmt.Sprintf("zzz-parity-chan-%d", ts)}, Auth: true,
	})
	if err != nil || catSnap.Status != http.StatusCreated {
		t.Fatalf("hazırlık kategorisi oluşturulamadı: %v %s", err, catSnap.Body)
	}
	var category struct {
		ID string `json:"id"`
	}
	_ = json.Unmarshal(catSnap.Body, &category)

	// Eşlenecek gerçek bir yaprak harici kategori bul.
	leafSnap, _ := r.send(Case{
		Method: http.MethodGet, Path: "/api/v1/channels/marketplaces/TY/categories?limit=50", Auth: true,
	})
	var externalCategories []struct {
		ExternalID string `json:"external_id"`
		IsLeaf     bool   `json:"is_leaf"`
	}
	_ = json.Unmarshal(leafSnap.Body, &externalCategories)
	leafExternalID := ""
	for _, candidate := range externalCategories {
		if candidate.IsLeaf {
			leafExternalID = candidate.ExternalID
			break
		}
	}

	run("category_mapping_unknown_external", Case{
		Name: "channels/category_mapping_unknown_external", Method: http.MethodPut,
		Path: "/api/v1/channels/marketplaces/TY/category-mappings/" + category.ID,
		Body: map[string]any{"external_id": "999999999"},
		Auth: true, Masks: problemMasks,
	})
	run("category_mapping_get_none", Case{
		Name: "channels/category_mapping_get_none", Method: http.MethodGet,
		Path: "/api/v1/channels/marketplaces/TY/category-mappings/" + category.ID,
		Auth: true, Masks: problemMasks,
	})

	mappingMasks := map[string]string{
		"id": MaskUUID, "catalog_category_id": MaskUUID, "external_id": MaskAnyString,
		"catalog_category.id": MaskUUID, "catalog_category.name": MaskAnyString,
		"catalog_category.code":        MaskNullable,
		"external_category.external_id": MaskAnyString, "external_category.name": MaskAnyString,
		"external_category.path": MaskAnyString, "external_category.synced_at": MaskDateTime,
	}
	if leafExternalID != "" {
		run("category_mapping_upsert", Case{
			Name: "channels/category_mapping_upsert", Method: http.MethodPut,
			Path: "/api/v1/channels/marketplaces/TY/category-mappings/" + category.ID,
			Body: map[string]any{"external_id": leafExternalID},
			Auth: true, Masks: mappingMasks,
		})
		run("category_mapping_get", Case{
			Name: "channels/category_mapping_get", Method: http.MethodGet,
			Path: "/api/v1/channels/marketplaces/TY/category-mappings/" + category.ID,
			Auth: true, Masks: mappingMasks,
		})

		// Alan eşlemesi: eşlenmemiş kategori/kaynak hataları.
		run("attribute_mapping_bad_source_type", Case{
			Name: "channels/attribute_mapping_bad_source_type", Method: http.MethodPut,
			Path: "/api/v1/channels/marketplaces/TY/category-mappings/" + category.ID + "/attribute-mappings",
			Body: map[string]any{"source_type": "yanlis", "catalog_source_id": category.ID, "external_attribute_id": "1"},
			Auth: true, Masks: problemMasks,
		})
		run("attribute_mapping_unknown_source", Case{
			Name: "channels/attribute_mapping_unknown_source", Method: http.MethodPut,
			Path: "/api/v1/channels/marketplaces/TY/category-mappings/" + category.ID + "/attribute-mappings",
			Body: map[string]any{"source_type": "catalog_attribute",
				"catalog_source_id": "00000000-0000-0000-0000-000000000009", "external_attribute_id": "1"},
			Auth: true, Masks: problemMasks,
		})
		run("attribute_mappings_list_empty", Case{
			Name: "channels/attribute_mappings_list_empty", Method: http.MethodGet,
			Path: "/api/v1/channels/marketplaces/TY/category-mappings/" + category.ID + "/attribute-mappings",
			Auth: true, Masks: map[string]string{"total_count": MaskAnyNumber, "total_pages": MaskAnyNumber},
		})
		run("attribute_mapping_get_unknown", Case{
			Name: "channels/attribute_mapping_get_unknown", Method: http.MethodGet,
			Path: "/api/v1/channels/marketplaces/TY/category-mappings/" + category.ID +
				"/attribute-mappings/00000000-0000-0000-0000-000000000009",
			Auth: true, Masks: problemMasks,
		})

		run("category_mapping_delete", Case{
			Name: "channels/category_mapping_delete", Method: http.MethodDelete,
			Path: "/api/v1/channels/marketplaces/TY/category-mappings/" + category.ID, Auth: true,
		})
	}

	// 5) Eşleme listesi (tenant genelinde; sayılar maskeli).
	run("category_mappings_list", Case{
		Name: "channels/category_mappings_list", Method: http.MethodGet,
		Path: "/api/v1/channels/marketplaces/TY/category-mappings?page=1&page_size=2", Auth: true,
		Masks: map[string]string{
			"items.*.id": MaskUUID, "items.*.catalog_category_id": MaskUUID,
			"items.*.external_id": MaskAnyString,
			"items.*.catalog_category.id": MaskUUID, "items.*.catalog_category.name": MaskAnyString,
			"items.*.catalog_category.code":         MaskNullable,
			"items.*.external_category.external_id": MaskAnyString,
			"items.*.external_category.name":        MaskAnyString,
			"items.*.external_category.path":        MaskAnyString,
			"items.*.external_category.synced_at":   MaskDateTime,
			"total_count":                           MaskAnyNumber,
			"total_pages":                           MaskAnyNumber,
		},
	})

	// 6) İmport ve yayın kuyruğu okuma uçları.
	run("imports_list", Case{
		Name: "channels/imports_list", Method: http.MethodGet,
		Path: "/api/v1/channels/marketplaces/TY/imports?limit=2", Auth: true,
		Masks: map[string]string{
			"*.id": MaskUUID, "*.created_at": MaskDateTime, "*.started_at": MaskNullable,
			"*.completed_at": MaskNullable, "*.total_products": MaskAnyNumber,
			"*.processed_products": MaskAnyNumber, "*.imported_products": MaskAnyNumber,
			"*.skipped_products": MaskAnyNumber, "*.failed_products": MaskAnyNumber,
			"*.status": MaskAnyString,
		},
	})
	run("import_run_unknown", Case{
		Name: "channels/import_run_unknown", Method: http.MethodGet,
		Path: "/api/v1/channels/marketplaces/TY/imports/00000000-0000-0000-0000-000000000009",
		Auth: true, Masks: problemMasks,
	})
	run("publication_run_unknown", Case{
		Name: "channels/publication_run_unknown", Method: http.MethodGet,
		Path: "/api/v1/channels/marketplaces/TY/publications/00000000-0000-0000-0000-000000000009",
		Auth: true, Masks: problemMasks,
	})

	// 7) Ürün hazırlık raporu (mevcut bir ürünle).
	prodSnap, _ := r.send(Case{
		Method: http.MethodGet, Path: "/api/v1/catalog/products?page=1&page_size=1", Auth: true,
	})
	var products struct {
		Items []struct {
			ID string `json:"id"`
		} `json:"items"`
	}
	_ = json.Unmarshal(prodSnap.Body, &products)
	if len(products.Items) > 0 {
		run("product_readiness", Case{
			Name: "channels/product_readiness", Method: http.MethodGet,
			Path: "/api/v1/channels/products/" + products.Items[0].ID + "/readiness", Auth: true,
			Masks: map[string]string{
				"product_id":                            MaskUUID,
				"channels.*.total_items":                MaskAnyNumber,
				"channels.*.items_missing_barcode":      MaskAnyNumber,
				"channels.*.missing_attributes.*.name":  MaskAnyString,
				"channels.*.missing_attributes.*.external_attribute_id": MaskAnyString,
				"channels.*.missing_attributes.*.missing_item_count":    MaskAnyNumber,
			},
		})
	}
	run("product_readiness_unknown", Case{
		Name: "channels/product_readiness_unknown", Method: http.MethodGet,
		Path: "/api/v1/channels/products/00000000-0000-0000-0000-000000000009/readiness",
		Auth: true, Masks: problemMasks,
	})
}
