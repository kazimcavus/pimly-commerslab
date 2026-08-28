// Package categories, hiyerarşik kategori kök varlığını ve kategoriye atanan
// öznitelik eşlemelerini içerir (.NET Catalog.Domain.Categories karşılığı).
package categories

import (
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// AttributeScope, kategoriye atanan özniteliğin ürün yapısındaki seviyesidir.
// Pazaryeri import'unda kaynak bayraklarından türetilir; kullanıcı sonradan
// düzenleyebilir. Kabloda ve veritabanında küçük harfli dizgi olarak taşınır.
type AttributeScope string

// Öznitelik seviyesi değerleri.
const (
	// ScopeModel: değer model (ürün) başına bir kez seçilir.
	ScopeModel AttributeScope = "model"

	// ScopeSlicer: değer slicer (ör. renk) değeri başına seçilir.
	ScopeSlicer AttributeScope = "slicer"

	// ScopeItem: değer satılabilir kalem (varyant) başına seçilir.
	ScopeItem AttributeScope = "item"
)

// ParseScope, kullanıcı girdisini seviyeye çözer (.NET Enum.TryParse
// ignoreCase karşılığı); tanınmayan/boş değer için ok=false döner ve çağıran
// varsayılanı seçer.
func ParseScope(value string) (AttributeScope, bool) {
	switch strings.ToLower(value) {
	case string(ScopeModel):
		return ScopeModel, true
	case string(ScopeSlicer):
		return ScopeSlicer, true
	case string(ScopeItem):
		return ScopeItem, true
	default:
		return "", false
	}
}

// Assignment, bir kategoriye bağlı özniteliğin zorunluluk ve sıralama
// kurallarını temsil eder (.NET CategoryAttributeAssignment karşılığı).
type Assignment struct {
	// ID, atamanın benzersiz kimliğidir.
	ID uuid.UUID

	// AttributeID, atanan öznitelik tanımının kimliğidir.
	AttributeID uuid.UUID

	// Required, özniteliğin bu kategoride zorunlu olup olmadığını belirtir.
	Required bool

	// SortOrder, kategori içindeki görüntüleme sırasıdır.
	SortOrder int

	// Scope, özniteliğin seçim seviyesidir.
	Scope AttributeScope
}

// Category, hiyerarşik yapıda kategorileri ve atanmış öznitelikleri yöneten
// kök varlıktır.
type Category struct {
	// ID, kategorinin benzersiz kimliğidir.
	ID uuid.UUID

	// Name, kategorinin görünen adıdır (en çok 500 karakter).
	Name string

	// Code, kategorinin opsiyonel kodudur.
	Code *string

	// ParentID, üst kategorinin kimliğidir; kök kategori için nil.
	ParentID *uuid.UUID

	// Assignments, kategoriye atanan öznitelik eşlemeleridir.
	Assignments []*Assignment
}

// NewCategory, doğrulanmış yeni bir kategori oluşturur.
func NewCategory(name string, code *string, parentID *uuid.UUID) sharedkernel.ResultOf[*Category] {
	if strings.TrimSpace(name) == "" {
		return sharedkernel.FailOf[*Category](sharedkernel.NewValidationError("Category name is required."))
	}
	return sharedkernel.OkOf(&Category{
		ID:       uuid.New(),
		Name:     strings.TrimSpace(name),
		Code:     normalizeCode(code),
		ParentID: parentID,
	})
}

// Rename, kategori adını ve opsiyonel kodunu günceller.
func (c *Category) Rename(name string, code *string) sharedkernel.Result {
	if strings.TrimSpace(name) == "" {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Category name is required."))
	}
	c.Name = strings.TrimSpace(name)
	c.Code = normalizeCode(code)
	return sharedkernel.Ok()
}

// MoveToParent, kategoriyi hiyerarşide başka bir üst kategoriye taşır;
// kendi kendine veya kendi alt soyuna taşınma döngü oluşturacağından reddedilir.
func (c *Category) MoveToParent(parentID *uuid.UUID, descendantIDs map[uuid.UUID]struct{}) sharedkernel.Result {
	if parentID != nil && *parentID == c.ID {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Category cannot be its own parent."))
	}
	if parentID != nil {
		if _, isDescendant := descendantIDs[*parentID]; isDescendant {
			return sharedkernel.Fail(sharedkernel.NewValidationError("Category cannot be moved under its own descendant."))
		}
	}
	c.ParentID = parentID
	return sharedkernel.Ok()
}

// AssignAttribute, kategoriye yeni bir öznitelik ataması ekler; aynı öznitelik
// ikinci kez atanamaz.
func (c *Category) AssignAttribute(attributeID uuid.UUID, required bool, sortOrder int, scope AttributeScope) sharedkernel.ResultOf[*Assignment] {
	for _, a := range c.Assignments {
		if a.AttributeID == attributeID {
			return sharedkernel.FailOf[*Assignment](
				sharedkernel.NewConflictError("Attribute is already assigned to this category."))
		}
	}
	assignment := &Assignment{
		ID:          uuid.New(),
		AttributeID: attributeID,
		Required:    required,
		SortOrder:   sortOrder,
		Scope:       scope,
	}
	c.Assignments = append(c.Assignments, assignment)
	return sharedkernel.OkOf(assignment)
}

// UpdateAssignment, mevcut bir atamanın kurallarını günceller; scope nil
// verilirse mevcut seviye korunur.
func (c *Category) UpdateAssignment(assignmentID uuid.UUID, required bool, sortOrder int, scope *AttributeScope) sharedkernel.Result {
	assignment := c.findAssignment(assignmentID)
	if assignment == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Category attribute assignment not found."))
	}
	assignment.Required = required
	assignment.SortOrder = sortOrder
	if scope != nil {
		assignment.Scope = *scope
	}
	return sharedkernel.Ok()
}

// RemoveAssignment, kategoriden bir öznitelik atamasını kaldırır.
func (c *Category) RemoveAssignment(assignmentID uuid.UUID) sharedkernel.Result {
	for i, a := range c.Assignments {
		if a.ID == assignmentID {
			c.Assignments = append(c.Assignments[:i], c.Assignments[i+1:]...)
			return sharedkernel.Ok()
		}
	}
	return sharedkernel.Fail(sharedkernel.NewNotFoundError("Category attribute assignment not found."))
}

// findAssignment, kimlikle atamayı döner; yoksa nil.
func (c *Category) findAssignment(id uuid.UUID) *Assignment {
	for _, a := range c.Assignments {
		if a.ID == id {
			return a
		}
	}
	return nil
}

// normalizeCode, kodu kırpar; boş kod nil'e çevrilir.
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
