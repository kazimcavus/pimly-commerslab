package parity

import (
	"encoding/json"
	"fmt"
	"net/http"
	"strings"
	"testing"
	"time"
)

// TestCatalogProductsParity, ürün uçlarının kablo formatı paritesini uçtan uca
// doğrular: toplu oluşturma (basit ürün), ürün/kalem/görsel yaşam döngüsü,
// benzersizlik çakışmaları ve SKU yapılandırma uçları. Kimlik/ad/barkod gibi
// koşuya özgü alanlar maskelenir.
func TestCatalogProductsParity(t *testing.T) {
	r := NewRunnerFromEnv("goldens")
	if r == nil {
		t.Skip("PARITY_BASE_URL tanımlı değil; parite testi atlandı")
	}
	if err := r.Login("owner@acme.test", "demo1234"); err != nil {
		t.Fatalf("koşucu girişi: %v", err)
	}

	ts := time.Now().UnixNano()
	barcode := fmt.Sprintf("999%d", ts%1_000_000_000_000)
	modelCode := fmt.Sprintf("ZZZPRT-%d", ts)
	productName := fmt.Sprintf("Zzz Parity Ürün %d", ts)
	groupID := "11111111-2222-3333-4444-555555555555"

	run := func(name string, c Case) *Snapshot {
		t.Helper()
		snap, err := r.RunWithResult(c)
		if err != nil {
			t.Errorf("%s: %v", name, err)
		}
		return &snap
	}

	// Önce bir kategori oluşturulur (ürün için zorunlu bağımlılık; golden'a girmez).
	catSnap, err := r.send(Case{
		Method: http.MethodPost, Path: "/api/v1/catalog/categories",
		Body: map[string]any{"name": fmt.Sprintf("zzz-parity-pcat-%d", ts)}, Auth: true,
	})
	if err != nil || catSnap.Status != http.StatusCreated {
		t.Fatalf("kategori oluşturulamadı: %v %s", err, catSnap.Body)
	}
	var category struct {
		ID string `json:"id"`
	}
	_ = json.Unmarshal(catSnap.Body, &category)

	// Basit ürün DTO maskeleri: kimlikler, koşuya özgü ad/kod/barkodlar.
	productMasks := map[string]string{
		"products.*.id": MaskUUID, "products.*.group_id": MaskUUID, "products.*.category_id": MaskUUID,
		"products.*.model_code": MaskAnyString, "products.*.name": MaskAnyString,
		"products.*.items.*.id": MaskUUID, "products.*.items.*.product_id": MaskUUID,
		"products.*.items.*.barcode": MaskAnyString,
	}

	// 1) Toplu oluşturma doğrulama: boş ürün listesi.
	run("prod_batch_validation_empty", Case{
		Name: "catalog/products_batch_validation_empty", Method: http.MethodPost,
		Path: "/api/v1/catalog/products:batch",
		Body: map[string]any{"group_id": groupID, "products": []any{}},
		Auth: true, Masks: problemMasks,
	})

	// 2) Toplu oluşturma doğrulama: sayısal olmayan barkod (alan adı biçimini de sabitler).
	run("prod_batch_validation_barcode", Case{
		Name: "catalog/products_batch_validation_barcode", Method: http.MethodPost,
		Path: "/api/v1/catalog/products:batch",
		Body: map[string]any{"group_id": groupID, "products": []any{map[string]any{
			"category_id": category.ID, "model_code": modelCode + "-V", "name": productName,
			"status": "active", "items": []any{map[string]any{"barcode": "ABC-123"}},
		}}},
		Auth: true, Masks: problemMasks,
	})

	// 3) Toplu oluşturma: eksensiz (basit) ürün, tek kalem.
	snap := run("prod_batch_create_basic", Case{
		Name: "catalog/products_batch_create_basic", Method: http.MethodPost,
		Path: "/api/v1/catalog/products:batch",
		Body: map[string]any{"group_id": groupID, "products": []any{map[string]any{
			"category_id": category.ID, "model_code": modelCode, "name": productName,
			"status": "draft", "items": []any{map[string]any{"barcode": barcode}},
		}}},
		Auth: true, Masks: productMasks,
	})
	var batch struct {
		Products []struct {
			ID    string `json:"id"`
			Items []struct {
				ID string `json:"id"`
			} `json:"items"`
		} `json:"products"`
	}
	if err := json.Unmarshal(snap.Body, &batch); err != nil || len(batch.Products) == 0 {
		t.Fatalf("toplu oluşturma yanıtı çözümlenemedi: %s", snap.Body)
	}
	productID := batch.Products[0].ID
	itemID := batch.Products[0].Items[0].ID

	singleMasks := map[string]string{
		"id": MaskUUID, "group_id": MaskUUID, "category_id": MaskUUID,
		"model_code": MaskAnyString, "name": MaskAnyString,
		"items.*.id": MaskUUID, "items.*.product_id": MaskUUID, "items.*.barcode": MaskAnyString,
		"images.*.id": MaskUUID, "images.*.url": MaskAnyString,
	}

	// 4) Aynı barkodla ikinci parti → kalıcı benzersizlik çakışması (mesaj barkod içerir → maskelenemez;
	//    çakışma mesajındaki barkod koşuya özgü olduğundan yalnızca durum/başlık karşılaştırılır).
	dupSnap, err := r.send(Case{
		Method: http.MethodPost, Path: "/api/v1/catalog/products:batch",
		Body: map[string]any{"group_id": groupID, "products": []any{map[string]any{
			"category_id": category.ID, "model_code": modelCode + "-2", "name": productName + "-2",
			"status": "draft", "items": []any{map[string]any{"barcode": barcode}},
		}}},
		Auth: true,
	})
	if err != nil || dupSnap.Status != http.StatusConflict {
		t.Errorf("barkod çakışması 409 beklerdi, %d geldi: %s", dupSnap.Status, dupSnap.Body)
	}

	// 5) Ürün getirme, listeleme ve güncelleme.
	run("prod_get", Case{
		Name: "catalog/products_get", Method: http.MethodGet,
		Path: "/api/v1/catalog/products/" + productID, Auth: true, Masks: singleMasks,
	})
	run("prod_update", Case{
		Name: "catalog/products_update", Method: http.MethodPatch,
		Path: "/api/v1/catalog/products/" + productID,
		Body: map[string]any{"category_id": category.ID, "name": productName + " (güncel)", "status": "active"},
		Auth: true, Masks: singleMasks,
	})
	run("prod_update_bad_status", Case{
		Name: "catalog/products_update_bad_status", Method: http.MethodPatch,
		Path: "/api/v1/catalog/products/" + productID,
		Body: map[string]any{"category_id": category.ID, "name": productName, "status": "yanlis"},
		Auth: true, Masks: problemMasks,
	})

	// 6) Kalem uçları.
	itemMasks := map[string]string{"id": MaskUUID, "product_id": MaskUUID, "barcode": MaskAnyString}
	run("prod_item_get", Case{
		Name: "catalog/product_items_get", Method: http.MethodGet,
		Path: "/api/v1/catalog/items/" + itemID, Auth: true, Masks: itemMasks,
	})
	run("prod_item_update", Case{
		Name: "catalog/product_items_update", Method: http.MethodPatch,
		Path: "/api/v1/catalog/items/" + itemID,
		Body: map[string]any{"gtin": "8690123456789", "mpn": "MP-PRT"},
		Auth: true, Masks: itemMasks,
	})
	run("prod_item_add_to_basic", Case{
		Name: "catalog/product_items_add_to_basic", Method: http.MethodPost,
		Path: "/api/v1/catalog/products/" + productID + "/items",
		Body: map[string]any{"barcode": barcode + "1"},
		Auth: true, Masks: problemMasks,
	})
	run("prod_item_delete_last", Case{
		Name: "catalog/product_items_delete_last", Method: http.MethodDelete,
		Path: "/api/v1/catalog/items/" + itemID, Auth: true, Masks: problemMasks,
	})

	// 7) Görsel uçları: geçersiz URL doğrulaması + geçerli akış.
	run("prod_image_bad_url", Case{
		Name: "catalog/product_images_bad_url", Method: http.MethodPost,
		Path: "/api/v1/catalog/products/" + productID + "/images",
		Body: map[string]any{"url": "https://baskasite.com/x.jpg", "sort_order": 1},
		Auth: true, Masks: problemMasks,
	})
	tenantHex := strings.ReplaceAll(r.TenantID, "-", "")
	mediaURL := "/media/" + tenantHex + "/aa/bb/parity.jpg"
	imgSnap := run("prod_image_add", Case{
		Name: "catalog/product_images_add", Method: http.MethodPost,
		Path: "/api/v1/catalog/products/" + productID + "/images",
		Body: map[string]any{"url": mediaURL, "sort_order": 1, "is_primary": true, "alt_text": "kapak"},
		Auth: true, Masks: map[string]string{"id": MaskUUID, "url": MaskAnyString},
	})
	var image struct {
		ID string `json:"id"`
	}
	_ = json.Unmarshal(imgSnap.Body, &image)
	if image.ID != "" {
		run("prod_image_update", Case{
			Name: "catalog/product_images_update", Method: http.MethodPatch,
			Path: "/api/v1/catalog/product-images/" + image.ID,
			Body: map[string]any{"url": mediaURL, "sort_order": 2, "is_primary": false},
			Auth: true, Masks: map[string]string{"id": MaskUUID, "url": MaskAnyString},
		})
		run("prod_image_delete", Case{
			Name: "catalog/product_images_delete", Method: http.MethodDelete,
			Path: "/api/v1/catalog/product-images/" + image.ID, Auth: true,
		})
	}

	// 8) Silme ve silinmiş kaynak davranışı.
	run("prod_delete", Case{
		Name: "catalog/products_delete", Method: http.MethodDelete,
		Path: "/api/v1/catalog/products/" + productID, Auth: true,
	})
	run("prod_get_after_delete", Case{
		Name: "catalog/products_get_after_delete", Method: http.MethodGet,
		Path: "/api/v1/catalog/products/" + productID, Auth: true, Masks: problemMasks,
	})

	// 9) SKU yapılandırma uçları.
	run("sku_config_get", Case{
		Name: "catalog/sku_config_get", Method: http.MethodGet,
		Path: "/api/v1/catalog/sku-config", Auth: true,
		Masks: map[string]string{"counter_next_value": MaskAnyNumber},
	})
	run("sku_config_put_validation", Case{
		Name: "catalog/sku_config_put_validation", Method: http.MethodPut,
		Path: "/api/v1/catalog/sku-config",
		Body: map[string]any{"enabled": true, "segments": []any{}},
		Auth: true, Masks: problemMasks,
	})
}
