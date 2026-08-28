package parity

import (
	"encoding/json"
	"fmt"
	"net/http"
	"testing"
	"time"
)

// TestCatalogVariantsParity, varyant türü ve değeri uçlarının kablo formatı
// paritesini akış halinde doğrular: tür oluşturma (anahtar türetme dahil),
// slicer teklik kuralı, değer ekleme/güncelleme (etiket+anahtar benzersizliği,
// medya URL doğrulaması) ve silme davranışları.
func TestCatalogVariantsParity(t *testing.T) {
	r := NewRunnerFromEnv("goldens")
	if r == nil {
		t.Skip("PARITY_BASE_URL tanımlı değil; parite testi atlandı")
	}
	if err := r.Login("owner@acme.test", "demo1234"); err != nil {
		t.Fatalf("koşucu girişi: %v", err)
	}

	ts := time.Now().UnixNano()
	typeName := fmt.Sprintf("Zzz Parity Renk %d", ts)
	typeMasks := map[string]string{"id": MaskUUID, "name": MaskAnyString, "key": MaskAnyString}
	valueMasks := map[string]string{"id": MaskUUID, "variant_type_id": MaskUUID}

	var typeID, valueID string
	run := func(name string, c Case) *Snapshot {
		t.Helper()
		snap, err := r.RunWithResult(c)
		if err != nil {
			t.Errorf("%s: %v", name, err)
		}
		return &snap
	}
	extractID := func(snap *Snapshot, target *string) {
		t.Helper()
		var body struct {
			ID string `json:"id"`
		}
		if err := json.Unmarshal(snap.Body, &body); err != nil || body.ID == "" {
			t.Fatalf("yanıttan kimlik çıkarılamadı: %s", snap.Body)
		}
		*target = body.ID
	}

	snap := run("var_create", Case{
		Name: "catalog/variants_create", Method: http.MethodPost,
		Path: "/api/v1/catalog/variants",
		Body: map[string]any{"name": typeName, "selection_style": "color", "sort_order": 5, "slicer": false},
		Auth: true, Masks: typeMasks,
	})
	extractID(snap, &typeID)

	run("var_create_name_conflict", Case{
		Name: "catalog/variants_create_name_conflict", Method: http.MethodPost,
		Path: "/api/v1/catalog/variants",
		Body: map[string]any{"name": typeName},
		Auth: true, Masks: problemMasks,
	})
	run("var_invalid_style", Case{
		Name: "catalog/variants_invalid_style", Method: http.MethodPost,
		Path: "/api/v1/catalog/variants",
		Body: map[string]any{"name": typeName + "-x", "selection_style": "swatch"},
		Auth: true, Masks: problemMasks,
	})
	run("var_get", Case{
		Name: "catalog/variants_get", Method: http.MethodGet,
		Path: "/api/v1/catalog/variants/" + typeID, Auth: true, Masks: typeMasks,
	})
	run("var_update", Case{
		Name: "catalog/variants_update", Method: http.MethodPatch,
		Path: "/api/v1/catalog/variants/" + typeID,
		Body: map[string]any{"name": typeName + "-updated", "selection_style": "list", "sort_order": 2, "slicer": false},
		Auth: true, Masks: typeMasks,
	})

	snap = run("var_add_value", Case{
		Name: "catalog/variants_add_value", Method: http.MethodPost,
		Path: "/api/v1/catalog/variants/" + typeID + "/values",
		Body: map[string]any{"label": "Kırmızı", "color": "#FF0000", "sort_order": 1},
		Auth: true, Masks: valueMasks,
	})
	extractID(snap, &valueID)

	run("var_add_value_label_conflict", Case{
		Name: "catalog/variants_add_value_label_conflict", Method: http.MethodPost,
		Path: "/api/v1/catalog/variants/" + typeID + "/values",
		Body: map[string]any{"label": "kırmızı", "sort_order": 2},
		Auth: true, Masks: problemMasks,
	})
	run("var_add_value_bad_image", Case{
		Name: "catalog/variants_add_value_bad_image", Method: http.MethodPost,
		Path: "/api/v1/catalog/variants/" + typeID + "/values",
		Body: map[string]any{"label": "Mavi", "image_url": "https://ornek.com/gorsel.jpg"},
		Auth: true, Masks: problemMasks,
	})
	run("var_list_values", Case{
		Name: "catalog/variants_list_values", Method: http.MethodGet,
		Path: "/api/v1/catalog/variants/" + typeID + "/values", Auth: true,
		Masks: map[string]string{"items.*.id": MaskUUID, "items.*.variant_type_id": MaskUUID},
	})
	run("var_update_value", Case{
		Name: "catalog/variants_update_value", Method: http.MethodPatch,
		Path: "/api/v1/catalog/variant-values/" + valueID,
		Body: map[string]any{"label": "Koyu Kırmızı", "color": "#CC0000", "sort_order": 1},
		Auth: true, Masks: valueMasks,
	})
	run("var_remove_value", Case{
		Name: "catalog/variants_remove_value", Method: http.MethodDelete,
		Path: "/api/v1/catalog/variant-values/" + valueID, Auth: true,
	})
	run("var_delete", Case{
		Name: "catalog/variants_delete", Method: http.MethodDelete,
		Path: "/api/v1/catalog/variants/" + typeID, Auth: true,
	})
	run("var_get_after_delete", Case{
		Name: "catalog/variants_get_after_delete", Method: http.MethodGet,
		Path: "/api/v1/catalog/variants/" + typeID, Auth: true, Masks: problemMasks,
	})
}
