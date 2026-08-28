package parity

import (
	"net/http"
	"testing"
)

// TestCatalogBarcodesSettingsParity, barkod serisi ve katalog ayarları
// uçlarının kablo formatı paritesini doğrular. Seri değerleri iki backend'in
// veritabanlarında farklı ilerlemiş olabileceğinden sayısal alanlar maskelenir.
func TestCatalogBarcodesSettingsParity(t *testing.T) {
	r := NewRunnerFromEnv("goldens")
	if r == nil {
		t.Skip("PARITY_BASE_URL tanımlı değil; parite testi atlandı")
	}
	if err := r.Login("owner@acme.test", "demo1234"); err != nil {
		t.Fatalf("koşucu girişi: %v", err)
	}

	run := func(name string, c Case) {
		t.Run(name, func(t *testing.T) {
			if err := r.Run(c); err != nil {
				t.Error(err)
			}
		})
	}

	sequenceMasks := map[string]string{
		"next_value": MaskAnyNumber, "next_preview": MaskAnyString,
	}

	// 1) Seri: get-or-create + doğrulama + tahsis akışı.
	run("barcode_sequence_get", Case{
		Name: "catalog/barcode_sequence_get", Method: http.MethodGet,
		Path: "/api/v1/catalog/barcode-sequence", Auth: true, Masks: sequenceMasks,
	})
	run("barcode_sequence_put_validation", Case{
		Name: "catalog/barcode_sequence_put_validation", Method: http.MethodPut,
		Path: "/api/v1/catalog/barcode-sequence",
		Body: map[string]any{"next_value": 0, "client_allocation_required": false},
		Auth: true, Masks: problemMasks,
	})
	run("barcodes_allocate", Case{
		Name: "catalog/barcodes_allocate", Method: http.MethodPost,
		Path: "/api/v1/catalog/barcodes:allocate",
		Body: map[string]any{"count": 3},
		Auth: true, Masks: map[string]string{"barcodes.*": MaskAnyString},
	})
	run("barcodes_allocate_too_many", Case{
		Name: "catalog/barcodes_allocate_too_many", Method: http.MethodPost,
		Path: "/api/v1/catalog/barcodes:allocate",
		Body: map[string]any{"count": 101},
		Auth: true, Masks: problemMasks,
	})
	run("barcode_allocations_list", Case{
		Name: "catalog/barcode_allocations_list", Method: http.MethodGet,
		Path: "/api/v1/catalog/barcode-allocations?page=1&page_size=2", Auth: true,
		Masks: map[string]string{
			"items.*.id": MaskUUID, "items.*.barcode": MaskAnyString,
			"items.*.allocated_at": MaskDateTime,
			"total_count":          MaskAnyNumber, "total_pages": MaskAnyNumber,
		},
	})
	// Tahsis edilmişin altına indirme çakışması: mevcut max bilinmediği için
	// yalnızca durum denetlenir (mesaj sayı içerir).
	lowSnap, err := r.send(Case{
		Method: http.MethodPut, Path: "/api/v1/catalog/barcode-sequence",
		Body: map[string]any{"next_value": 1, "client_allocation_required": false}, Auth: true,
	})
	if err != nil || lowSnap.Status != http.StatusConflict {
		t.Errorf("seri geri alma 409 beklerdi, %d geldi: %s", lowSnap.Status, lowSnap.Body)
	}

	// 2) Ayarlar: get-or-create + güncelleme + doğrulama.
	run("settings_get", Case{
		Name: "catalog/settings_get", Method: http.MethodGet,
		Path: "/api/v1/catalog/settings", Auth: true,
		Masks: map[string]string{"slicer_name_position": MaskAnyString},
	})
	run("settings_put_prefix", Case{
		Name: "catalog/settings_put_prefix", Method: http.MethodPut,
		Path: "/api/v1/catalog/settings",
		Body: map[string]any{"slicer_name_position": "prefix"},
		Auth: true,
	})
	run("settings_put_invalid", Case{
		Name: "catalog/settings_put_invalid", Method: http.MethodPut,
		Path: "/api/v1/catalog/settings",
		Body: map[string]any{"slicer_name_position": "ortada"},
		Auth: true, Masks: problemMasks,
	})
	run("settings_put_suffix", Case{
		Name: "catalog/settings_put_suffix", Method: http.MethodPut,
		Path: "/api/v1/catalog/settings",
		Body: map[string]any{"slicer_name_position": "suffix"},
		Auth: true,
	})
}
