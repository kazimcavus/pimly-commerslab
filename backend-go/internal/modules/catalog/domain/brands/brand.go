// Package brands, ürünlerin bağlanabileceği düz (hiyerarşisiz) marka kök
// varlığını içerir (.NET Catalog.Domain.Brands karşılığı).
package brands

import (
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Brand, markayı yöneten kök varlıktır. Code, opsiyonel marka kodudur
// (ör. pazaryeri marka kimliği); boş dizgi yerine nil saklanır.
type Brand struct {
	// ID, markanın benzersiz kimliğidir.
	ID uuid.UUID

	// Name, markanın görünen adıdır (en çok 500 karakter).
	Name string

	// Code, markanın opsiyonel kodudur; yokluğu nil ile temsil edilir.
	Code *string
}

// NewBrand, doğrulanmış yeni bir marka oluşturur; ad ve kod kırpılır,
// boş kod nil'e çevrilir. Hata mesajı .NET karşılığıyla birebir aynıdır.
func NewBrand(name string, code *string) sharedkernel.ResultOf[*Brand] {
	if strings.TrimSpace(name) == "" {
		return sharedkernel.FailOf[*Brand](sharedkernel.NewValidationError("Brand name is required."))
	}
	return sharedkernel.OkOf(&Brand{
		ID:   uuid.New(),
		Name: strings.TrimSpace(name),
		Code: normalizeCode(code),
	})
}

// Rename, marka adını ve opsiyonel kodunu günceller.
func (b *Brand) Rename(name string, code *string) sharedkernel.Result {
	if strings.TrimSpace(name) == "" {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Brand name is required."))
	}
	b.Name = strings.TrimSpace(name)
	b.Code = normalizeCode(code)
	return sharedkernel.Ok()
}

// normalizeCode, kodu kırpar; boş/beyaz boşluk kodu nil'e çevirir.
func normalizeCode(code *string) *string {
	if code == nil {
		return nil
	}
	trimmed := strings.TrimSpace(*code)
	if trimmed == "" {
		return nil
	}
	return &trimmed
}
