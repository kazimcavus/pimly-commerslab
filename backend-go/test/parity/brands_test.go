package parity

import (
	"encoding/json"
	"fmt"
	"net/http"
	"testing"
	"time"
)

// TestCatalogBrandsParity, marka uçlarının kablo formatı paritesini akış
// halinde doğrular: oluştur → getir → güncelle → listele → sil → tekrar getir.
// Her koşuda benzersiz adlar kullanılır (iki backend'in veritabanları farklı
// kayıtlar biriktirir); ad alanları bu yüzden maskelenir. Liste senaryosu
// page_size=1 ile alfabetik ilk kaydı ister — o kayıt her iki tarafta da aynı
// import verisinden gelir, test markaları "zzz-" önekiyle sona düşer.
func TestCatalogBrandsParity(t *testing.T) {
	r := NewRunnerFromEnv("goldens")
	if r == nil {
		t.Skip("PARITY_BASE_URL tanımlı değil; parite testi atlandı")
	}
	if err := r.Login("owner@acme.test", "demo1234"); err != nil {
		t.Fatalf("koşucu girişi: %v", err)
	}

	unique := fmt.Sprintf("zzz-parity-%d", time.Now().UnixNano())
	createMasks := map[string]string{"id": MaskUUID, "name": MaskAnyString}
	created := struct {
		ID string `json:"id"`
	}{}

	run := func(name string, c Case) {
		t.Run(name, func(t *testing.T) {
			if err := r.Run(c); err != nil {
				t.Error(err)
			}
		})
	}

	// 1) Oluşturma — kimlik akışın geri kalanında kullanılır.
	t.Run("catalog/brands_create", func(t *testing.T) {
		snap, err := r.RunWithResult(Case{
			Name:   "catalog/brands_create",
			Method: http.MethodPost,
			Path:   "/api/v1/catalog/brands",
			Body:   map[string]any{"name": unique, "code": "PRT-01"},
			Auth:   true,
			Masks:  createMasks,
		})
		if err != nil {
			t.Fatal(err)
		}
		if err := json.Unmarshal(snap.Body, &created); err != nil {
			t.Fatalf("oluşturma yanıtı çözümlenemedi: %v", err)
		}
	})
	if created.ID == "" {
		t.Fatal("marka oluşturulamadı; akışın devamı çalıştırılamaz")
	}

	// 2) Getirme ve güncelleme.
	run("catalog/brands_get", Case{
		Name: "catalog/brands_get", Method: http.MethodGet,
		Path: "/api/v1/catalog/brands/" + created.ID, Auth: true, Masks: createMasks,
	})
	run("catalog/brands_update", Case{
		Name: "catalog/brands_update", Method: http.MethodPatch,
		Path: "/api/v1/catalog/brands/" + created.ID,
		Body: map[string]any{"name": unique + "-updated", "code": nil},
		Auth: true, Masks: createMasks,
	})

	// 3) Çakışma: import verisinden her iki tarafta da var olan bir adla.
	run("catalog/brands_create_conflict", Case{
		Name: "catalog/brands_create_conflict", Method: http.MethodPost,
		Path: "/api/v1/catalog/brands",
		Body: map[string]any{"name": unique + "-updated"},
		Auth: true, Masks: problemMasks,
	})

	// 4) Doğrulama: boş ad + uzunluk aşımı tek istekte.
	longCode := make([]byte, 101)
	for i := range longCode {
		longCode[i] = 'x'
	}
	run("catalog/brands_validation", Case{
		Name: "catalog/brands_validation", Method: http.MethodPost,
		Path: "/api/v1/catalog/brands",
		Body: map[string]any{"name": "", "code": string(longCode)},
		Auth: true, Masks: problemMasks,
	})

	// 5) Liste zarfı: alfabetik ilk kayıt (import verisi, iki tarafta aynı).
	run("catalog/brands_list_first", Case{
		Name: "catalog/brands_list_first", Method: http.MethodGet,
		Path: "/api/v1/catalog/brands?page=1&page_size=1",
		Auth: true,
		Masks: map[string]string{
			"items.*.id":   MaskUUID,
			"items.*.name": MaskAnyString,
			"total_count":  MaskAnyNumber, "total_pages": MaskAnyNumber,
		},
	})

	// 6) Bilinmeyen kimlik ve geçersiz kimlik biçimi.
	run("catalog/brands_get_unknown", Case{
		Name: "catalog/brands_get_unknown", Method: http.MethodGet,
		Path: "/api/v1/catalog/brands/00000000-0000-0000-0000-000000000001",
		Auth: true, Masks: problemMasks,
	})
	run("catalog/brands_get_invalid_id", Case{
		Name: "catalog/brands_get_invalid_id", Method: http.MethodGet,
		Path: "/api/v1/catalog/brands/not-a-guid", Auth: true,
	})

	// 7) Silme ve silinmişi getirme.
	run("catalog/brands_delete", Case{
		Name: "catalog/brands_delete", Method: http.MethodDelete,
		Path: "/api/v1/catalog/brands/" + created.ID, Auth: true,
	})
	run("catalog/brands_get_after_delete", Case{
		Name: "catalog/brands_get_after_delete", Method: http.MethodGet,
		Path: "/api/v1/catalog/brands/" + created.ID, Auth: true, Masks: problemMasks,
	})
}
