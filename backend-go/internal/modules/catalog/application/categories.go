package application

import (
	"context"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/attributes"
	"pimly.commerslab/backend-go/internal/modules/catalog/domain/categories"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// CategoryDto, kategori veri transfer nesnesidir.
type CategoryDto struct {
	ID       uuid.UUID  `json:"id"`
	Name     string     `json:"name"`
	Code     *string    `json:"code"`
	ParentID *uuid.UUID `json:"parent_id"`
}

// CategoryAttributeDto, kategori-özellik ataması veri transfer nesnesidir;
// scope kabloda "model" | "slicer" | "item" olarak taşınır.
type CategoryAttributeDto struct {
	CategoryAttributeID uuid.UUID `json:"category_attribute_id"`
	AttributeID         uuid.UUID `json:"attribute_id"`
	Key                 string    `json:"key"`
	Name                string    `json:"name"`
	Required            bool      `json:"required"`
	SortOrder           int       `json:"sort_order"`
	Scope               string    `json:"scope"`
}

// categoryToDto, domain kategorisini DTO'ya çevirir.
func categoryToDto(c *categories.Category) CategoryDto {
	return CategoryDto{ID: c.ID, Name: c.Name, Code: c.Code, ParentID: c.ParentID}
}

// assignmentToDto, atama + özellik çiftini DTO'ya çevirir
// (.NET CategoryAttributeMapping.ToDto karşılığı).
func assignmentToDto(a *categories.Assignment, attr *attributes.Attribute) CategoryAttributeDto {
	return CategoryAttributeDto{
		CategoryAttributeID: a.ID,
		AttributeID:         attr.ID,
		Key:                 attr.Key,
		Name:                attr.Name,
		Required:            a.Required,
		SortOrder:           a.SortOrder,
		Scope:               string(a.Scope),
	}
}

// CategoryRepository, kategori kalıcılık portudur (.NET ICategoryRepository
// karşılığı). Tüm metodlar tenant kimliğini açıkça alır.
type CategoryRepository interface {
	// GetByID, kategoriyi atamalarıyla birlikte döner; yoksa nil.
	GetByID(ctx context.Context, tenantID, id uuid.UUID) (*categories.Category, error)

	// List, kategorileri ada göre sıralı ve sayfalanmış listeler.
	List(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[*categories.Category], error)

	// GetDescendantIDs, kategorinin tüm alt soy kimliklerini döner
	// (taşıma sırasında döngü denetimi için).
	GetDescendantIDs(ctx context.Context, tenantID, categoryID uuid.UUID) (map[uuid.UUID]struct{}, error)

	// FindByAssignmentID, atama kimliğinin ait olduğu kategoriyi (atamalarıyla)
	// döner; yoksa nil (.NET CategoryAssignmentLookup karşılığı).
	FindByAssignmentID(ctx context.Context, tenantID, assignmentID uuid.UUID) (*categories.Category, error)

	// Add, yeni kategoriyi (varsa atamalarıyla) ekler.
	Add(ctx context.Context, tenantID uuid.UUID, category *categories.Category) error

	// Update, kategori alanlarını ve atama koleksiyonunu kalıcılaştırır
	// (eklenen/güncellenen/silinen atamalar tek işlemde eşitlenir).
	Update(ctx context.Context, tenantID uuid.UUID, category *categories.Category) error

	// Remove, kategoriyi siler (atamalar veritabanında cascade silinir,
	// alt kategorilerin parent_id'si null'a düşer).
	Remove(ctx context.Context, tenantID, id uuid.UUID) error
}

// CreateCategoryCommand, yeni kategori isteğini taşır.
type CreateCategoryCommand struct {
	Name     string
	Code     *string
	ParentID *uuid.UUID
}

// UpdateCategoryCommand, kategori güncelleme isteğini taşır; ParentID nil ise
// kategori kök seviyeye taşınır.
type UpdateCategoryCommand struct {
	ID       uuid.UUID
	Name     string
	Code     *string
	ParentID *uuid.UUID
}

// AssignCategoryAttributeCommand, kategoriye özellik atama isteğini taşır.
type AssignCategoryAttributeCommand struct {
	CategoryID  uuid.UUID
	AttributeID uuid.UUID
	Required    bool
	SortOrder   int
	Scope       categories.AttributeScope
}

// UpdateCategoryAttributeCommand, atama güncelleme isteğini taşır; Scope nil
// ise mevcut seviye korunur.
type UpdateCategoryAttributeCommand struct {
	ID        uuid.UUID
	Required  bool
	SortOrder int
	Scope     *categories.AttributeScope
}

// validateCategoryInput, kategori ad/kod kurallarını uygular
// (.NET Create/UpdateCategoryCommandValidator portu).
func validateCategoryInput(name string, code *string) *sharedkernel.Error {
	var f fieldErrors
	f.required("name", "Name", name)
	f.maxLength("name", "Name", name, CategoryNameMaxLength)
	f.maxLength("code", "Code", deref(code), CategoryCodeMaxLength)
	return f.failure()
}

// CategoryHandlers, kategori kullanım senaryolarını yürütür (.NET'teki dokuz
// ayrı handler sınıfının Go karşılığı).
type CategoryHandlers struct {
	categories CategoryRepository
	attributes AttributeRepository
}

// NewCategoryHandlers, bağımlılıklarıyla kategori handler'larını oluşturur.
func NewCategoryHandlers(categories CategoryRepository, attributes AttributeRepository) *CategoryHandlers {
	return &CategoryHandlers{categories: categories, attributes: attributes}
}

// Create, yeni kategori oluşturur; verilen üst kategori yoksa not_found döner.
func (h *CategoryHandlers) Create(ctx context.Context, tenantID uuid.UUID, cmd CreateCategoryCommand) sharedkernel.ResultOf[CategoryDto] {
	if verr := validateCategoryInput(cmd.Name, cmd.Code); verr != nil {
		return sharedkernel.FailOf[CategoryDto](verr)
	}

	if cmd.ParentID != nil {
		parent, err := h.categories.GetByID(ctx, tenantID, *cmd.ParentID)
		if err != nil {
			return sharedkernel.FailOf[CategoryDto](sharedkernel.NewInternalError(err.Error()))
		}
		if parent == nil {
			return sharedkernel.FailOf[CategoryDto](sharedkernel.NewNotFoundError("Parent category not found."))
		}
	}

	createResult := categories.NewCategory(cmd.Name, cmd.Code, cmd.ParentID)
	if createResult.IsFailure() {
		return sharedkernel.FailOf[CategoryDto](createResult.Err())
	}
	category := createResult.Value()

	if err := h.categories.Add(ctx, tenantID, category); err != nil {
		return sharedkernel.FailOf[CategoryDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(categoryToDto(category))
}

// List, kategorileri ada göre sıralı sayfalar halinde döner.
func (h *CategoryHandlers) List(ctx context.Context, tenantID uuid.UUID, page, pageSize int) sharedkernel.ResultOf[sharedkernel.PagedResult[CategoryDto]] {
	pr := sharedkernel.ResolvePagination(page, pageSize)
	if pr.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[CategoryDto]](pr.Err())
	}
	pageResult, err := h.categories.List(ctx, tenantID, pr.Value())
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[CategoryDto]](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(sharedkernel.MapPagedResult(pageResult, categoryToDto))
}

// Get, tek kategoriyi döner; yoksa not_found.
func (h *CategoryHandlers) Get(ctx context.Context, tenantID, id uuid.UUID) sharedkernel.ResultOf[CategoryDto] {
	category, err := h.categories.GetByID(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.FailOf[CategoryDto](sharedkernel.NewInternalError(err.Error()))
	}
	if category == nil {
		return sharedkernel.FailOf[CategoryDto](sharedkernel.NewNotFoundError("Category not found."))
	}
	return sharedkernel.OkOf(categoryToDto(category))
}

// Update, kategoriyi yeniden adlandırır ve/veya hiyerarşide taşır; döngü
// oluşturacak taşımalar reddedilir.
func (h *CategoryHandlers) Update(ctx context.Context, tenantID uuid.UUID, cmd UpdateCategoryCommand) sharedkernel.ResultOf[CategoryDto] {
	if verr := validateCategoryInput(cmd.Name, cmd.Code); verr != nil {
		return sharedkernel.FailOf[CategoryDto](verr)
	}

	category, err := h.categories.GetByID(ctx, tenantID, cmd.ID)
	if err != nil {
		return sharedkernel.FailOf[CategoryDto](sharedkernel.NewInternalError(err.Error()))
	}
	if category == nil {
		return sharedkernel.FailOf[CategoryDto](sharedkernel.NewNotFoundError("Category not found."))
	}

	if cmd.ParentID != nil && (category.ParentID == nil || *cmd.ParentID != *category.ParentID) {
		parent, err := h.categories.GetByID(ctx, tenantID, *cmd.ParentID)
		if err != nil {
			return sharedkernel.FailOf[CategoryDto](sharedkernel.NewInternalError(err.Error()))
		}
		if parent == nil {
			return sharedkernel.FailOf[CategoryDto](sharedkernel.NewNotFoundError("Parent category not found."))
		}
	}

	if renameResult := category.Rename(cmd.Name, cmd.Code); renameResult.IsFailure() {
		return sharedkernel.FailOf[CategoryDto](renameResult.Err())
	}

	descendants, err := h.categories.GetDescendantIDs(ctx, tenantID, category.ID)
	if err != nil {
		return sharedkernel.FailOf[CategoryDto](sharedkernel.NewInternalError(err.Error()))
	}
	if moveResult := category.MoveToParent(cmd.ParentID, descendants); moveResult.IsFailure() {
		return sharedkernel.FailOf[CategoryDto](moveResult.Err())
	}

	if err := h.categories.Update(ctx, tenantID, category); err != nil {
		return sharedkernel.FailOf[CategoryDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(categoryToDto(category))
}

// Delete, kategoriyi siler; yoksa not_found.
func (h *CategoryHandlers) Delete(ctx context.Context, tenantID, id uuid.UUID) sharedkernel.Result {
	category, err := h.categories.GetByID(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if category == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Category not found."))
	}
	if err := h.categories.Remove(ctx, tenantID, id); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}

// AssignAttribute, kategoriye özellik atar; kategori/özellik yoksa not_found,
// aynı özellik zaten atanmışsa conflict döner.
func (h *CategoryHandlers) AssignAttribute(ctx context.Context, tenantID uuid.UUID, cmd AssignCategoryAttributeCommand) sharedkernel.ResultOf[CategoryAttributeDto] {
	category, err := h.categories.GetByID(ctx, tenantID, cmd.CategoryID)
	if err != nil {
		return sharedkernel.FailOf[CategoryAttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if category == nil {
		return sharedkernel.FailOf[CategoryAttributeDto](sharedkernel.NewNotFoundError("Category not found."))
	}

	attribute, err := h.attributes.GetByID(ctx, tenantID, cmd.AttributeID)
	if err != nil {
		return sharedkernel.FailOf[CategoryAttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if attribute == nil {
		return sharedkernel.FailOf[CategoryAttributeDto](sharedkernel.NewNotFoundError("Attribute not found."))
	}

	assignResult := category.AssignAttribute(cmd.AttributeID, cmd.Required, cmd.SortOrder, cmd.Scope)
	if assignResult.IsFailure() {
		return sharedkernel.FailOf[CategoryAttributeDto](assignResult.Err())
	}

	if err := h.categories.Update(ctx, tenantID, category); err != nil {
		return sharedkernel.FailOf[CategoryAttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(assignmentToDto(assignResult.Value(), attribute))
}

// ListAttributes, kategorinin özellik atamalarını sıralama alanına göre
// sayfalanmış döner.
func (h *CategoryHandlers) ListAttributes(ctx context.Context, tenantID, categoryID uuid.UUID, page, pageSize int) sharedkernel.ResultOf[sharedkernel.PagedResult[CategoryAttributeDto]] {
	pr := sharedkernel.ResolvePagination(page, pageSize)
	if pr.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[CategoryAttributeDto]](pr.Err())
	}

	category, err := h.categories.GetByID(ctx, tenantID, categoryID)
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[CategoryAttributeDto]](sharedkernel.NewInternalError(err.Error()))
	}
	if category == nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[CategoryAttributeDto]](sharedkernel.NewNotFoundError("Category not found."))
	}

	rows, err := h.assignmentRows(ctx, tenantID, category)
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[CategoryAttributeDto]](sharedkernel.NewInternalError(err.Error()))
	}

	p := pr.Value()
	total := len(rows)
	start := min(p.Skip(), total)
	end := min(start+p.PageSize, total)
	return sharedkernel.OkOf(sharedkernel.NewPagedResult(rows[start:end], p, total))
}

// assignmentRows, kategorinin atamalarını SortOrder'a göre sıralı DTO listesine
// çevirir; özelliği silinmiş atamalar .NET davranışıyla uyumlu olarak atlanır.
func (h *CategoryHandlers) assignmentRows(ctx context.Context, tenantID uuid.UUID, category *categories.Category) ([]CategoryAttributeDto, error) {
	sorted := make([]*categories.Assignment, len(category.Assignments))
	copy(sorted, category.Assignments)
	for i := 1; i < len(sorted); i++ {
		for j := i; j > 0 && sorted[j-1].SortOrder > sorted[j].SortOrder; j-- {
			sorted[j-1], sorted[j] = sorted[j], sorted[j-1]
		}
	}

	rows := []CategoryAttributeDto{}
	for _, assignment := range sorted {
		attribute, err := h.attributes.GetByID(ctx, tenantID, assignment.AttributeID)
		if err != nil {
			return nil, err
		}
		if attribute == nil {
			continue
		}
		rows = append(rows, assignmentToDto(assignment, attribute))
	}
	return rows, nil
}

// UpdateAssignment, atamanın zorunluluk/sıralama/seviye kurallarını günceller.
func (h *CategoryHandlers) UpdateAssignment(ctx context.Context, tenantID uuid.UUID, cmd UpdateCategoryAttributeCommand) sharedkernel.ResultOf[CategoryAttributeDto] {
	owner, err := h.categories.FindByAssignmentID(ctx, tenantID, cmd.ID)
	if err != nil {
		return sharedkernel.FailOf[CategoryAttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if owner == nil {
		return sharedkernel.FailOf[CategoryAttributeDto](sharedkernel.NewNotFoundError("Category attribute assignment not found."))
	}

	if updateResult := owner.UpdateAssignment(cmd.ID, cmd.Required, cmd.SortOrder, cmd.Scope); updateResult.IsFailure() {
		return sharedkernel.FailOf[CategoryAttributeDto](updateResult.Err())
	}
	if err := h.categories.Update(ctx, tenantID, owner); err != nil {
		return sharedkernel.FailOf[CategoryAttributeDto](sharedkernel.NewInternalError(err.Error()))
	}

	var updated *categories.Assignment
	for _, a := range owner.Assignments {
		if a.ID == cmd.ID {
			updated = a
			break
		}
	}
	attribute, err := h.attributes.GetByID(ctx, tenantID, updated.AttributeID)
	if err != nil {
		return sharedkernel.FailOf[CategoryAttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if attribute == nil {
		return sharedkernel.FailOf[CategoryAttributeDto](sharedkernel.NewNotFoundError("Attribute not found."))
	}
	return sharedkernel.OkOf(assignmentToDto(updated, attribute))
}

// RemoveAssignment, atamayı kategoriden kaldırır.
func (h *CategoryHandlers) RemoveAssignment(ctx context.Context, tenantID, assignmentID uuid.UUID) sharedkernel.Result {
	owner, err := h.categories.FindByAssignmentID(ctx, tenantID, assignmentID)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if owner == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Category attribute assignment not found."))
	}
	if removeResult := owner.RemoveAssignment(assignmentID); removeResult.IsFailure() {
		return sharedkernel.Fail(removeResult.Err())
	}
	if err := h.categories.Update(ctx, tenantID, owner); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}
