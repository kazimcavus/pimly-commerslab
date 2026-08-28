// Package attributes, ürünlere eklenecek özellik tanımlarının kök varlığını
// içerir (.NET Catalog.Domain.Attributes karşılığı). Özellik adını ve
// seçilebilir değerlerini yönetir; anahtar (Key) oluşturulurken adından
// türetilir: "Yaka Tipi" → YAKA_TIPI.
package attributes

import (
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/keygen"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Value, bir özelliğe ait seçilebilir değerdir (.NET AttributeValue karşılığı).
type Value struct {
	// ID, değerin benzersiz kimliğidir.
	ID uuid.UUID

	// Name, değerin görünen adıdır (özellik içinde büyük/küçük harf duyarsız benzersiz).
	Name string
}

// Attribute, özellik tanımının kök varlığıdır.
type Attribute struct {
	// ID, özelliğin benzersiz kimliğidir.
	ID uuid.UUID

	// Key, özelliği benzersiz tanımlayan anahtardır; adından türetilir (YAKA_TIPI).
	Key string

	// Name, özelliğin kullanıcıya gösterilen adıdır (Yaka Tipi).
	Name string

	// Values, özelliğe ait seçilebilir değerlerdir.
	Values []*Value
}

// NewAttribute, doğrulanmış yeni bir özellik oluşturur; anahtar adından türetilir.
func NewAttribute(name string) sharedkernel.ResultOf[*Attribute] {
	if strings.TrimSpace(name) == "" {
		return sharedkernel.FailOf[*Attribute](sharedkernel.NewValidationError("Attribute name is required."))
	}
	trimmed := strings.TrimSpace(name)
	keyResult := keygen.FromName(trimmed)
	if keyResult.IsFailure() {
		return sharedkernel.FailOf[*Attribute](keyResult.Err())
	}
	return sharedkernel.OkOf(&Attribute{ID: uuid.New(), Key: keyResult.Value(), Name: trimmed})
}

// Rename, özelliğin görünen adını günceller (anahtar değişmez).
func (a *Attribute) Rename(name string) sharedkernel.Result {
	if strings.TrimSpace(name) == "" {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Attribute name is required."))
	}
	a.Name = strings.TrimSpace(name)
	return sharedkernel.Ok()
}

// AddValue, özelliğe yeni değer ekler; ad özellik içinde büyük/küçük harf
// duyarsız benzersiz olmalıdır.
func (a *Attribute) AddValue(name string) sharedkernel.ResultOf[*Value] {
	if strings.TrimSpace(name) == "" {
		return sharedkernel.FailOf[*Value](sharedkernel.NewValidationError("Attribute value name is required."))
	}
	trimmed := strings.TrimSpace(name)
	for _, v := range a.Values {
		if strings.EqualFold(v.Name, trimmed) {
			return sharedkernel.FailOf[*Value](
				sharedkernel.NewConflictError("Attribute value name must be unique within the attribute."))
		}
	}
	value := &Value{ID: uuid.New(), Name: trimmed}
	a.Values = append(a.Values, value)
	return sharedkernel.OkOf(value)
}

// UpdateValue, mevcut bir değerin adını günceller; benzersizlik korunur.
func (a *Attribute) UpdateValue(valueID uuid.UUID, name string) sharedkernel.Result {
	if strings.TrimSpace(name) == "" {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Attribute value name is required."))
	}
	value := a.findValue(valueID)
	if value == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Attribute value not found."))
	}
	trimmed := strings.TrimSpace(name)
	for _, v := range a.Values {
		if v.ID != valueID && strings.EqualFold(v.Name, trimmed) {
			return sharedkernel.Fail(
				sharedkernel.NewConflictError("Attribute value name must be unique within the attribute."))
		}
	}
	value.Name = trimmed
	return sharedkernel.Ok()
}

// RemoveValue, özellikten bir değeri kaldırır.
func (a *Attribute) RemoveValue(valueID uuid.UUID) sharedkernel.Result {
	for i, v := range a.Values {
		if v.ID == valueID {
			a.Values = append(a.Values[:i], a.Values[i+1:]...)
			return sharedkernel.Ok()
		}
	}
	return sharedkernel.Fail(sharedkernel.NewNotFoundError("Attribute value not found."))
}

// findValue, kimlikle değeri döner; yoksa nil.
func (a *Attribute) findValue(id uuid.UUID) *Value {
	for _, v := range a.Values {
		if v.ID == id {
			return v
		}
	}
	return nil
}
