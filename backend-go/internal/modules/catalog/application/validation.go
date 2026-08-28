// Package application, Catalog modülünün kullanım senaryosu handler'larını,
// doğrulayıcılarını ve DTO sözleşmelerini içerir (.NET Catalog.Application
// karşılığı). Doğrulama kodları ve mesaj şablonları kablo formatının
// parçasıdır; .NET CatalogValidationRules/ValidationMessages ile birebir aynıdır.
package application

import (
	"fmt"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Alan uzunluk sınırları (.NET CatalogValidationRules sabitleri).
const (
	CategoryNameMaxLength       = 500
	CategoryCodeMaxLength       = 100
	BrandNameMaxLength          = 500
	BrandCodeMaxLength          = 100
	AttributeNameMaxLength      = 500
	AttributeValueNameMaxLength = 200
)

// fieldErrors, doğrulama hatalarını biriktiren yardımcı türdür; kurallar
// FluentValidation gibi kısa devre yapmadan sırayla uygulanır.
type fieldErrors struct {
	errs []sharedkernel.ValidationError
}

// required, boş değeri "required" koduyla işaretler
// (mesaj: "{Field} is required.").
func (f *fieldErrors) required(field, display, value string) {
	if value == "" {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: field, Code: sharedkernel.ValidationCodeRequired,
			Message: fmt.Sprintf("%s is required.", display)})
	}
}

// maxLength, sınırı aşan değeri "max_length" koduyla işaretler
// (mesaj: "{Field} must not exceed {N} characters.").
func (f *fieldErrors) maxLength(field, display, value string, limit int) {
	if len([]rune(value)) > limit {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: field, Code: sharedkernel.ValidationCodeMaxLength,
			Message: fmt.Sprintf("%s must not exceed %d characters.", display, limit)})
	}
}

// requiredID, boş GUID'i "invalid_id" koduyla işaretler
// (.NET RequiredId kuralı; mesaj: "{Field} must be a valid identifier.").
func (f *fieldErrors) requiredID(field, display string, value uuid.UUID) {
	if value == uuid.Nil {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: field, Code: sharedkernel.ValidationCodeInvalidID,
			Message: fmt.Sprintf("%s must be a valid identifier.", display)})
	}
}

// failure, biriken hataları .NET ile aynı özet mesajlı doğrulama hatasına
// çevirir; hata yoksa nil döner.
func (f *fieldErrors) failure() *sharedkernel.Error {
	if len(f.errs) == 0 {
		return nil
	}
	return sharedkernel.NewValidationError("One or more validation errors occurred.", f.errs...)
}

// deref, opsiyonel dizgiyi doğrulama için boş dizgiye indirger.
func deref(s *string) string {
	if s == nil {
		return ""
	}
	return *s
}
