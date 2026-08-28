// Package variants, ürün varyasyon eksenlerini (renk, beden vb.) tanımlayan
// kök varlığı içerir (.NET Catalog.Domain.Variants karşılığı). SKU
// kombinasyonunu değil; eksen adını, seçim stilini, sıralamasını ve
// seçilebilir değerleri yönetir.
package variants

import (
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/keygen"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// SelectionStyle, varyant değerlerinin kullanıcı arayüzünde nasıl sunulacağını
// belirler; kabloda ve veritabanında küçük harfli dizgi taşınır ("list"/"color").
type SelectionStyle string

// Seçim stilleri.
const (
	// StyleList: değerler düz liste olarak sunulur.
	StyleList SelectionStyle = "list"

	// StyleColor: değerler renk yuvarlakları olarak sunulur.
	StyleColor SelectionStyle = "color"
)

// ParseSelectionStyle, kullanıcı girdisini stile çözer (.NET Enum.Parse
// ignoreCase karşılığı); tanınmayan değer için ok=false döner.
func ParseSelectionStyle(value string) (SelectionStyle, bool) {
	switch strings.ToLower(strings.TrimSpace(value)) {
	case string(StyleList):
		return StyleList, true
	case string(StyleColor):
		return StyleColor, true
	default:
		return "", false
	}
}

// Value, bir varyant türüne ait seçilebilir değerdir; etiket, opsiyonel renk
// ve görsel bilgileriyle (.NET VariantValue karşılığı).
type Value struct {
	// ID, değerin benzersiz kimliğidir.
	ID uuid.UUID

	// Key, değeri benzersiz tanımlayan anahtardır; verilmezse etiketten türetilir.
	Key string

	// Label, değerin görünen etiketidir (tür içinde büyük/küçük harf duyarsız benzersiz).
	Label string

	// Color, görsel gösterim için renk kodudur; opsiyonel.
	Color *string

	// ImageURL, görsel gösterim için resim adresidir; opsiyonel.
	ImageURL *string

	// SortOrder, tür içindeki görüntüleme sırasıdır.
	SortOrder int
}

// Variant, varyant türünün kök varlığıdır.
type Variant struct {
	// ID, türün benzersiz kimliğidir.
	ID uuid.UUID

	// Key, türü benzersiz tanımlayan anahtardır (RENK); verilmezse adından türetilir.
	Key string

	// Name, türün görünen adıdır (Renk).
	Name string

	// SelectionStyle, kullanıcı arayüzü seçim stilidir.
	SelectionStyle SelectionStyle

	// SortOrder, ürün içindeki görüntüleme sırasıdır.
	SortOrder int

	// Slicer, türün filtreleme (slicer) ekseni olup olmadığını belirtir;
	// tenant başına en fazla bir tür slicer olabilir.
	Slicer bool

	// Values, türe ait seçilebilir değerlerdir.
	Values []*Value
}

// keyFromOptional, açık anahtar verilmişse doğrular, verilmemişse addan türetir
// (.NET VariantKey.FromOptional karşılığı).
func keyFromOptional(key *string, fallbackName string) sharedkernel.ResultOf[string] {
	if key == nil || strings.TrimSpace(*key) == "" {
		return keygen.FromName(fallbackName)
	}
	return keygen.ValidateExplicit(*key)
}

// NewVariant, doğrulanmış yeni bir varyant türü oluşturur.
func NewVariant(name string, style SelectionStyle, sortOrder int, slicer bool, key *string) sharedkernel.ResultOf[*Variant] {
	if strings.TrimSpace(name) == "" {
		return sharedkernel.FailOf[*Variant](sharedkernel.NewValidationError("Variant type name is required."))
	}
	trimmed := strings.TrimSpace(name)
	keyResult := keyFromOptional(key, trimmed)
	if keyResult.IsFailure() {
		return sharedkernel.FailOf[*Variant](keyResult.Err())
	}
	return sharedkernel.OkOf(&Variant{
		ID:             uuid.New(),
		Key:            keyResult.Value(),
		Name:           trimmed,
		SelectionStyle: style,
		SortOrder:      sortOrder,
		Slicer:         slicer,
	})
}

// Rename, türün adını, seçim stilini, sırasını ve slicer bayrağını günceller
// (anahtar değişmez).
func (v *Variant) Rename(name string, style SelectionStyle, sortOrder int, slicer bool) sharedkernel.Result {
	if strings.TrimSpace(name) == "" {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Variant type name is required."))
	}
	v.Name = strings.TrimSpace(name)
	v.SelectionStyle = style
	v.SortOrder = sortOrder
	v.Slicer = slicer
	return sharedkernel.Ok()
}

// AddValue, türe yeni değer ekler; etiket ve anahtar tür içinde büyük/küçük
// harf duyarsız benzersiz olmalıdır.
func (v *Variant) AddValue(label string, color, imageURL, key *string, sortOrder int) sharedkernel.ResultOf[*Value] {
	if strings.TrimSpace(label) == "" {
		return sharedkernel.FailOf[*Value](sharedkernel.NewValidationError("Variant value label is required."))
	}
	trimmedLabel := strings.TrimSpace(label)
	for _, existing := range v.Values {
		if strings.EqualFold(existing.Label, trimmedLabel) {
			return sharedkernel.FailOf[*Value](
				sharedkernel.NewConflictError("Variant value label must be unique within the type."))
		}
	}

	keyResult := keyFromOptional(key, trimmedLabel)
	if keyResult.IsFailure() {
		return sharedkernel.FailOf[*Value](keyResult.Err())
	}
	for _, existing := range v.Values {
		if strings.EqualFold(existing.Key, keyResult.Value()) {
			return sharedkernel.FailOf[*Value](
				sharedkernel.NewConflictError("Variant value key must be unique within the type."))
		}
	}

	value := &Value{
		ID:        uuid.New(),
		Key:       keyResult.Value(),
		Label:     trimmedLabel,
		Color:     normalizeOptional(color),
		ImageURL:  normalizeOptional(imageURL),
		SortOrder: sortOrder,
	}
	v.Values = append(v.Values, value)
	return sharedkernel.OkOf(value)
}

// UpdateValue, mevcut değerin etiket/görsel/anahtar/sıra bilgilerini günceller;
// benzersizlik kuralları korunur.
func (v *Variant) UpdateValue(valueID uuid.UUID, label string, color, imageURL, key *string, sortOrder int) sharedkernel.Result {
	if strings.TrimSpace(label) == "" {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Variant value label is required."))
	}
	value := v.findValue(valueID)
	if value == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Variant value not found."))
	}

	trimmedLabel := strings.TrimSpace(label)
	for _, existing := range v.Values {
		if existing.ID != valueID && strings.EqualFold(existing.Label, trimmedLabel) {
			return sharedkernel.Fail(sharedkernel.NewConflictError("Variant value label must be unique within the type."))
		}
	}
	keyResult := keyFromOptional(key, trimmedLabel)
	if keyResult.IsFailure() {
		return sharedkernel.Fail(keyResult.Err())
	}
	for _, existing := range v.Values {
		if existing.ID != valueID && strings.EqualFold(existing.Key, keyResult.Value()) {
			return sharedkernel.Fail(sharedkernel.NewConflictError("Variant value key must be unique within the type."))
		}
	}

	value.Key = keyResult.Value()
	value.Label = trimmedLabel
	value.Color = normalizeOptional(color)
	value.ImageURL = normalizeOptional(imageURL)
	value.SortOrder = sortOrder
	return sharedkernel.Ok()
}

// RemoveValue, türden bir değeri kaldırır.
func (v *Variant) RemoveValue(valueID uuid.UUID) sharedkernel.Result {
	for i, existing := range v.Values {
		if existing.ID == valueID {
			v.Values = append(v.Values[:i], v.Values[i+1:]...)
			return sharedkernel.Ok()
		}
	}
	return sharedkernel.Fail(sharedkernel.NewNotFoundError("Variant value not found."))
}

// findValue, kimlikle değeri döner; yoksa nil.
func (v *Variant) findValue(id uuid.UUID) *Value {
	for _, existing := range v.Values {
		if existing.ID == id {
			return existing
		}
	}
	return nil
}

// normalizeOptional, opsiyonel dizgiyi kırpar; boş değer nil'e çevrilir.
func normalizeOptional(s *string) *string {
	if s == nil {
		return nil
	}
	trimmed := strings.TrimSpace(*s)
	if trimmed == "" {
		return nil
	}
	return &trimmed
}
