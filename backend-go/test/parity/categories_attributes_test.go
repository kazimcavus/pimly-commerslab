package parity

import (
	"encoding/json"
	"fmt"
	"net/http"
	"testing"
	"time"
)

// TestCatalogCategoriesAttributesParity, kategori + özellik uçlarının kablo
// formatı paritesini uçtan uca bir akışla doğrular: kategori hiyerarşisi,
// özellik tanımı/değerleri ve kategori-özellik atamaları. Benzersiz adlar
// (zaman damgalı) kullanılır ve ad/anahtar alanları maskelenir.
func TestCatalogCategoriesAttributesParity(t *testing.T) {
	r := NewRunnerFromEnv("goldens")
	if r == nil {
		t.Skip("PARITY_BASE_URL tanımlı değil; parite testi atlandı")
	}
	if err := r.Login("owner@acme.test", "demo1234"); err != nil {
		t.Fatalf("koşucu girişi: %v", err)
	}

	ts := time.Now().UnixNano()
	catName := fmt.Sprintf("zzz-parity-cat-%d", ts)
	attrName := fmt.Sprintf("Zzz Parity Attr %d", ts)

	dtoMasks := map[string]string{"id": MaskUUID, "name": MaskAnyString}
	var rootID, childID, attrID, valueID, assignmentID string

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

	// --- Kategoriler ---
	snap := run("cat_create_root", Case{
		Name: "catalog/categories_create_root", Method: http.MethodPost,
		Path: "/api/v1/catalog/categories",
		Body: map[string]any{"name": catName, "code": "PRT-CAT"},
		Auth: true, Masks: dtoMasks,
	})
	extractID(snap, &rootID)

	snap = run("cat_create_child", Case{
		Name: "catalog/categories_create_child", Method: http.MethodPost,
		Path: "/api/v1/catalog/categories",
		Body: map[string]any{"name": catName + "-child", "parent_id": rootID},
		Auth: true, Masks: map[string]string{"id": MaskUUID, "name": MaskAnyString, "parent_id": MaskUUID},
	})
	extractID(snap, &childID)

	run("cat_get", Case{
		Name: "catalog/categories_get", Method: http.MethodGet,
		Path: "/api/v1/catalog/categories/" + childID, Auth: true,
		Masks: map[string]string{"id": MaskUUID, "name": MaskAnyString, "parent_id": MaskUUID},
	})
	run("cat_parent_not_found", Case{
		Name: "catalog/categories_parent_not_found", Method: http.MethodPost,
		Path: "/api/v1/catalog/categories",
		Body: map[string]any{"name": catName + "-x", "parent_id": "00000000-0000-0000-0000-000000000009"},
		Auth: true, Masks: problemMasks,
	})
	run("cat_self_parent", Case{
		Name: "catalog/categories_self_parent", Method: http.MethodPatch,
		Path: "/api/v1/catalog/categories/" + rootID,
		Body: map[string]any{"name": catName, "parent_id": rootID},
		Auth: true, Masks: problemMasks,
	})
	run("cat_move_under_descendant", Case{
		Name: "catalog/categories_move_under_descendant", Method: http.MethodPatch,
		Path: "/api/v1/catalog/categories/" + rootID,
		Body: map[string]any{"name": catName, "parent_id": childID},
		Auth: true, Masks: problemMasks,
	})
	run("cat_update_move_root", Case{
		Name: "catalog/categories_update_move_root", Method: http.MethodPatch,
		Path: "/api/v1/catalog/categories/" + childID,
		Body: map[string]any{"name": catName + "-child-renamed", "code": "C2"},
		Auth: true, Masks: dtoMasks,
	})
	run("cat_validation", Case{
		Name: "catalog/categories_validation", Method: http.MethodPost,
		Path: "/api/v1/catalog/categories",
		Body: map[string]any{"name": ""},
		Auth: true, Masks: problemMasks,
	})
	run("cat_list_first", Case{
		Name: "catalog/categories_list_first", Method: http.MethodGet,
		Path: "/api/v1/catalog/categories?page=1&page_size=1", Auth: true,
		Masks: map[string]string{
			"items.*.id": MaskUUID, "items.*.name": MaskAnyString,
			"items.*.parent_id": MaskNullable, "items.*.code": MaskNullable,
			"total_count": MaskAnyNumber, "total_pages": MaskAnyNumber,
		},
	})

	// --- Özellikler ---
	snap = run("attr_create", Case{
		Name: "catalog/attributes_create", Method: http.MethodPost,
		Path: "/api/v1/catalog/attributes",
		Body: map[string]any{"name": attrName},
		Auth: true, Masks: map[string]string{"id": MaskUUID, "name": MaskAnyString, "key": MaskAnyString},
	})
	extractID(snap, &attrID)

	run("attr_create_conflict", Case{
		Name: "catalog/attributes_create_conflict", Method: http.MethodPost,
		Path: "/api/v1/catalog/attributes",
		Body: map[string]any{"name": attrName},
		Auth: true, Masks: problemMasks,
	})

	snap = run("attr_add_value", Case{
		Name: "catalog/attributes_add_value", Method: http.MethodPost,
		Path: "/api/v1/catalog/attributes/" + attrID + "/values",
		Body: map[string]any{"name": "Parity Value A"},
		Auth: true, Masks: map[string]string{"id": MaskUUID, "attribute_id": MaskUUID},
	})
	extractID(snap, &valueID)

	run("attr_add_value_duplicate", Case{
		Name: "catalog/attributes_add_value_duplicate", Method: http.MethodPost,
		Path: "/api/v1/catalog/attributes/" + attrID + "/values",
		Body: map[string]any{"name": "parity value a"},
		Auth: true, Masks: problemMasks,
	})
	run("attr_list_values", Case{
		Name: "catalog/attributes_list_values", Method: http.MethodGet,
		Path: "/api/v1/catalog/attributes/" + attrID + "/values", Auth: true,
		Masks: map[string]string{"items.*.id": MaskUUID, "items.*.attribute_id": MaskUUID},
	})
	run("attr_update_value", Case{
		Name: "catalog/attributes_update_value", Method: http.MethodPatch,
		Path: "/api/v1/catalog/attribute-values/" + valueID,
		Body: map[string]any{"name": "Parity Value B"},
		Auth: true, Masks: map[string]string{"id": MaskUUID, "attribute_id": MaskUUID},
	})

	// --- Kategori-özellik atamaları ---
	snap = run("cat_assign_attr", Case{
		Name: "catalog/category_attributes_assign", Method: http.MethodPost,
		Path: "/api/v1/catalog/categories/" + rootID + "/attributes",
		Body: map[string]any{"attribute_id": attrID, "required": true, "sort_order": 3, "scope": "slicer"},
		Auth: true, Masks: map[string]string{
			"category_attribute_id": MaskUUID, "attribute_id": MaskUUID,
			"key": MaskAnyString, "name": MaskAnyString,
		},
	})
	var assignBody struct {
		CategoryAttributeID string `json:"category_attribute_id"`
	}
	if err := json.Unmarshal(snap.Body, &assignBody); err != nil || assignBody.CategoryAttributeID == "" {
		t.Fatalf("atama kimliği çıkarılamadı: %s", snap.Body)
	}
	assignmentID = assignBody.CategoryAttributeID

	run("cat_assign_attr_duplicate", Case{
		Name: "catalog/category_attributes_assign_duplicate", Method: http.MethodPost,
		Path: "/api/v1/catalog/categories/" + rootID + "/attributes",
		Body: map[string]any{"attribute_id": attrID, "required": false, "sort_order": 1},
		Auth: true, Masks: problemMasks,
	})
	run("cat_list_attrs", Case{
		Name: "catalog/category_attributes_list", Method: http.MethodGet,
		Path: "/api/v1/catalog/categories/" + rootID + "/attributes", Auth: true,
		Masks: map[string]string{
			"items.*.category_attribute_id": MaskUUID, "items.*.attribute_id": MaskUUID,
			"items.*.key": MaskAnyString, "items.*.name": MaskAnyString,
		},
	})
	run("cat_update_assignment", Case{
		Name: "catalog/category_attributes_update", Method: http.MethodPatch,
		Path: "/api/v1/catalog/category-attributes/" + assignmentID,
		Body: map[string]any{"required": false, "sort_order": 7, "scope": "item"},
		Auth: true, Masks: map[string]string{
			"category_attribute_id": MaskUUID, "attribute_id": MaskUUID,
			"key": MaskAnyString, "name": MaskAnyString,
		},
	})
	run("cat_update_assignment_keeps_scope", Case{
		Name: "catalog/category_attributes_update_keeps_scope", Method: http.MethodPatch,
		Path: "/api/v1/catalog/category-attributes/" + assignmentID,
		Body: map[string]any{"required": true, "sort_order": 2, "scope": "bilinmeyen"},
		Auth: true, Masks: map[string]string{
			"category_attribute_id": MaskUUID, "attribute_id": MaskUUID,
			"key": MaskAnyString, "name": MaskAnyString,
		},
	})
	run("cat_remove_assignment", Case{
		Name: "catalog/category_attributes_remove", Method: http.MethodDelete,
		Path: "/api/v1/catalog/category-attributes/" + assignmentID, Auth: true,
	})

	// --- Temizlik + silinmiş kaynak davranışları ---
	run("attr_remove_value", Case{
		Name: "catalog/attributes_remove_value", Method: http.MethodDelete,
		Path: "/api/v1/catalog/attribute-values/" + valueID, Auth: true,
	})
	run("attr_delete", Case{
		Name: "catalog/attributes_delete", Method: http.MethodDelete,
		Path: "/api/v1/catalog/attributes/" + attrID, Auth: true,
	})
	run("cat_delete_child", Case{
		Name: "catalog/categories_delete_child", Method: http.MethodDelete,
		Path: "/api/v1/catalog/categories/" + childID, Auth: true,
	})
	run("cat_delete_root", Case{
		Name: "catalog/categories_delete_root", Method: http.MethodDelete,
		Path: "/api/v1/catalog/categories/" + rootID, Auth: true,
	})
	run("cat_get_after_delete", Case{
		Name: "catalog/categories_get_after_delete", Method: http.MethodGet,
		Path: "/api/v1/catalog/categories/" + rootID, Auth: true, Masks: problemMasks,
	})
}
