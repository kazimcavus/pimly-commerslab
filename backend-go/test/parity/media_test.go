package parity

import (
	"encoding/json"
	"net/http"
	"testing"
)

// tinyPNG, geçerli bir 1x1 PNG dosyasıdır (magic-byte denetimini geçer).
var tinyPNG = []byte{
	0x89, 'P', 'N', 'G', 0x0D, 0x0A, 0x1A, 0x0A,
	0x00, 0x00, 0x00, 0x0D, 'I', 'H', 'D', 'R',
	0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
	0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89,
	0x00, 0x00, 0x00, 0x0A, 'I', 'D', 'A', 'T',
	0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01,
	0x0D, 0x0A, 0x2D, 0xB4,
	0x00, 0x00, 0x00, 0x00, 'I', 'E', 'N', 'D', 0xAE, 0x42, 0x60, 0x82,
}

// TestMediaParity, görsel yükleme ucunun kablo formatı paritesini doğrular:
// başarılı PNG yüklemesi (url maskeli, content_type/size_bytes birebir),
// desteklenmeyen biçim reddi ve boş dosya reddi. Yüklenen dosyanın /media
// statik yolundan gerçekten sunulduğu da denetlenir.
func TestMediaParity(t *testing.T) {
	r := NewRunnerFromEnv("goldens")
	if r == nil {
		t.Skip("PARITY_BASE_URL tanımlı değil; parite testi atlandı")
	}
	if err := r.Login("owner@acme.test", "demo1234"); err != nil {
		t.Fatalf("koşucu girişi: %v", err)
	}

	// 1) Başarılı yükleme.
	snap, err := r.RunWithResult(Case{
		Name: "media/uploads_png", Method: http.MethodPost,
		Path: "/api/v1/media/uploads?purpose=product",
		FileBytes: tinyPNG, FileName: "parity.png",
		Auth: true, Masks: map[string]string{"url": MaskAnyString},
	})
	if err != nil {
		t.Fatal(err)
	}
	var upload struct {
		URL string `json:"url"`
	}
	_ = json.Unmarshal(snap.Body, &upload)

	// 2) Yüklenen dosya /media statik yolundan sunulmalı (aynı bayt sayısı).
	if upload.URL != "" {
		served, err := r.send(Case{Method: http.MethodGet, Path: upload.URL})
		if err != nil || served.Status != http.StatusOK || len(served.Body) != len(tinyPNG) {
			t.Errorf("/media sunumu başarısız: status=%d boyut=%d beklenen=%d hata=%v",
				served.Status, len(served.Body), len(tinyPNG), err)
		}
	}

	// 3) Desteklenmeyen biçim reddi.
	if err := r.Run(Case{
		Name: "media/uploads_invalid_format", Method: http.MethodPost,
		Path: "/api/v1/media/uploads",
		FileBytes: []byte("bu bir resim degil"), FileName: "notes.txt",
		Auth: true, Masks: problemMasks,
	}); err != nil {
		t.Error(err)
	}

	// 4) Dosyasız istek reddi.
	if err := r.Run(Case{
		Name: "media/uploads_missing_file", Method: http.MethodPost,
		Path: "/api/v1/media/uploads",
		FileBytes: []byte{}, FileName: "empty.png",
		Auth: true, Masks: problemMasks,
	}); err != nil {
		t.Error(err)
	}
}
