// Package application, Media modülünün görsel yükleme kullanım senaryosunu
// içerir (.NET Media.Application karşılığı). Depolama yereldir; içerik türü
// istemci beyanıyla değil magic-byte koklamasıyla belirlenir ve yalnızca
// JPEG/PNG/WebP kabul edilir.
package application

import (
	"context"
	"fmt"
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Yükleme amacı boyut limitleri (.NET MediaValidationRules sabitleri).
const (
	// SwatchMaxBytes, varyant swatch görselinin azami boyutudur (512 KB).
	SwatchMaxBytes = 512 * 1024

	// ProductMaxBytes, ürün galerisi görselinin azami boyutudur (5 MB).
	ProductMaxBytes = 5 * 1024 * 1024
)

// Purpose, görsel yükleme amacıdır; boyut limitini belirler.
type Purpose string

// Yükleme amaçları.
const (
	// PurposeProduct: ürün galerisi görseli (5 MB sınır).
	PurposeProduct Purpose = "product"

	// PurposeSwatch: varyant swatch görseli (512 KB sınır).
	PurposeSwatch Purpose = "swatch"
)

// ParsePurpose, sorgu parametresini amaca çözer; "swatch" dışındaki her değer
// (boş dahil) product'a düşer (.NET ParsePurpose davranışı).
func ParsePurpose(value string) Purpose {
	if strings.ToLower(strings.TrimSpace(value)) == string(PurposeSwatch) {
		return PurposeSwatch
	}
	return PurposeProduct
}

// maxBytes, amacın boyut sınırını döner.
func (p Purpose) maxBytes() int64 {
	if p == PurposeSwatch {
		return SwatchMaxBytes
	}
	return ProductMaxBytes
}

// UploadImageResultDto, yükleme yanıtının kablo biçimidir.
type UploadImageResultDto struct {
	URL         string `json:"url"`
	ContentType string `json:"content_type"`
	SizeBytes   int64  `json:"size_bytes"`
}

// StoredBlob, depolanan dosyanın anahtar/tür/boyut bilgisidir.
type StoredBlob struct {
	StorageKey  string
	ContentType string
	SizeBytes   int64
}

// BlobStorage, blob depolama portudur (.NET IBlobStorage karşılığı).
type BlobStorage interface {
	// Save, içeriği tenant'a özel parçalı yola yazar ve depolama bilgisini döner.
	Save(ctx context.Context, content []byte, contentType string, tenantID uuid.UUID) (StoredBlob, error)

	// Delete, depolama anahtarındaki dosyayı siler; dosya yoksa sessizce geçer.
	Delete(ctx context.Context, storageKey string) error
}

// DetectImageContentType, magic-byte koklamasıyla görsel MIME türünü belirler
// (.NET ImageContentTypeDetector portu); desteklenmeyen biçim için boş döner.
func DetectImageContentType(content []byte) string {
	if len(content) >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF {
		return "image/jpeg"
	}
	if len(content) >= 8 &&
		content[0] == 0x89 && content[1] == 'P' && content[2] == 'N' && content[3] == 'G' &&
		content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A {
		return "image/png"
	}
	if len(content) >= 12 &&
		content[0] == 'R' && content[1] == 'I' && content[2] == 'F' && content[3] == 'F' &&
		content[8] == 'W' && content[9] == 'E' && content[10] == 'B' && content[11] == 'P' {
		return "image/webp"
	}
	return ""
}

// UploadHandlers, görsel yükleme kullanım senaryosunu yürütür.
type UploadHandlers struct {
	storage       BlobStorage
	publicBaseURL string
}

// NewUploadHandlers, bağımlılıklarıyla handler'ı oluşturur; publicBaseURL boşsa
// URL'ler göreli (/media/...) üretilir.
func NewUploadHandlers(storage BlobStorage, publicBaseURL string) *UploadHandlers {
	return &UploadHandlers{storage: storage, publicBaseURL: publicBaseURL}
}

// Upload, görseli doğrulayıp depolar ve genel erişim URL'sini döner
// (.NET UploadImageHandler portu).
func (h *UploadHandlers) Upload(ctx context.Context, tenantID uuid.UUID, content []byte, purpose Purpose) sharedkernel.ResultOf[UploadImageResultDto] {
	if int64(len(content)) > purpose.maxBytes() {
		return sharedkernel.FailOf[UploadImageResultDto](sharedkernel.NewValidationError(
			"One or more validation errors occurred.",
			sharedkernel.ValidationError{
				Field: "", Code: "out_of_range",
				Message: fmt.Sprintf("File must not exceed %d bytes.", purpose.maxBytes()),
			}))
	}

	contentType := DetectImageContentType(content)
	if contentType == "" {
		return sharedkernel.FailOf[UploadImageResultDto](sharedkernel.NewValidationError(
			"Unsupported or invalid image format."))
	}

	stored, err := h.storage.Save(ctx, content, contentType, tenantID)
	if err != nil {
		return sharedkernel.FailOf[UploadImageResultDto](sharedkernel.NewInternalError(err.Error()))
	}

	url := "/media/" + stored.StorageKey
	if strings.TrimSpace(h.publicBaseURL) != "" {
		url = strings.TrimRight(h.publicBaseURL, "/") + url
	}
	return sharedkernel.OkOf(UploadImageResultDto{
		URL: url, ContentType: stored.ContentType, SizeBytes: stored.SizeBytes})
}
