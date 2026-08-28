package application

import (
	"context"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/attributes"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// AttributeDto, özellik tanımı veri transfer nesnesidir.
type AttributeDto struct {
	ID   uuid.UUID `json:"id"`
	Key  string    `json:"key"`
	Name string    `json:"name"`
}

// AttributeDefinitionValueDto, özellik tanımı değeri veri transfer nesnesidir.
type AttributeDefinitionValueDto struct {
	ID          uuid.UUID `json:"id"`
	AttributeID uuid.UUID `json:"attribute_id"`
	Name        string    `json:"name"`
}

// attributeToDto, domain özelliğini DTO'ya çevirir.
func attributeToDto(a *attributes.Attribute) AttributeDto {
	return AttributeDto{ID: a.ID, Key: a.Key, Name: a.Name}
}

// valueToDto, özellik değerini DTO'ya çevirir.
func valueToDto(v *attributes.Value, attributeID uuid.UUID) AttributeDefinitionValueDto {
	return AttributeDefinitionValueDto{ID: v.ID, AttributeID: attributeID, Name: v.Name}
}

// AttributeRepository, özellik kalıcılık portudur (.NET IAttributeRepository
// karşılığı). Tüm metodlar tenant kimliğini açıkça alır.
type AttributeRepository interface {
	// GetByID, özelliği değerleriyle birlikte döner; yoksa nil.
	GetByID(ctx context.Context, tenantID, id uuid.UUID) (*attributes.Attribute, error)

	// GetByKey, özelliği anahtarına göre döner; yoksa nil.
	GetByKey(ctx context.Context, tenantID uuid.UUID, key string) (*attributes.Attribute, error)

	// List, özellikleri anahtara göre sıralı ve sayfalanmış listeler.
	List(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[*attributes.Attribute], error)

	// FindByValueID, değer kimliğinin ait olduğu özelliği (değerleriyle) döner;
	// yoksa nil (.NET AttributeLookup karşılığı).
	FindByValueID(ctx context.Context, tenantID, valueID uuid.UUID) (*attributes.Attribute, error)

	// Add, yeni özelliği (varsa değerleriyle) ekler.
	Add(ctx context.Context, tenantID uuid.UUID, attribute *attributes.Attribute) error

	// Update, özellik alanlarını ve değer koleksiyonunu kalıcılaştırır.
	Update(ctx context.Context, tenantID uuid.UUID, attribute *attributes.Attribute) error

	// Remove, özelliği siler (değerler veritabanında cascade silinir).
	Remove(ctx context.Context, tenantID, id uuid.UUID) error
}

// CreateAttributeCommand, yeni özellik isteğini taşır.
type CreateAttributeCommand struct{ Name string }

// UpdateAttributeCommand, özellik yeniden adlandırma isteğini taşır.
type UpdateAttributeCommand struct {
	ID   uuid.UUID
	Name string
}

// AddAttributeValueCommand, özelliğe değer ekleme isteğini taşır.
type AddAttributeValueCommand struct {
	AttributeID uuid.UUID
	Name        string
}

// UpdateAttributeValueCommand, değer güncelleme isteğini taşır.
type UpdateAttributeValueCommand struct {
	ID   uuid.UUID
	Name string
}

// validateAttributeName, özellik adı kurallarını uygular (.NET AttributeName kuralı).
func validateAttributeName(name string) *sharedkernel.Error {
	var f fieldErrors
	f.required("name", "Name", name)
	f.maxLength("name", "Name", name, AttributeNameMaxLength)
	return f.failure()
}

// validateAttributeValueInput, değer ekleme kurallarını uygular
// (.NET AddAttributeValueCommandValidator: RequiredId + AttributeValueName).
func validateAttributeValueInput(attributeID uuid.UUID, name string) *sharedkernel.Error {
	var f fieldErrors
	f.requiredID("attribute_id", "AttributeId", attributeID)
	f.required("name", "Name", name)
	f.maxLength("name", "Name", name, AttributeValueNameMaxLength)
	return f.failure()
}

// validateAttributeValueName, değer güncelleme kurallarını uygular.
func validateAttributeValueName(name string) *sharedkernel.Error {
	var f fieldErrors
	f.required("name", "Name", name)
	f.maxLength("name", "Name", name, AttributeValueNameMaxLength)
	return f.failure()
}

// AttributeHandlers, özellik kullanım senaryolarını yürütür (.NET'teki dokuz
// ayrı handler sınıfının Go karşılığı).
type AttributeHandlers struct {
	attributes AttributeRepository
}

// NewAttributeHandlers, bağımlılıklarıyla özellik handler'larını oluşturur.
func NewAttributeHandlers(attributes AttributeRepository) *AttributeHandlers {
	return &AttributeHandlers{attributes: attributes}
}

// Create, yeni özellik oluşturur; adından türetilen anahtar zaten varsa
// conflict döner.
func (h *AttributeHandlers) Create(ctx context.Context, tenantID uuid.UUID, cmd CreateAttributeCommand) sharedkernel.ResultOf[AttributeDto] {
	if verr := validateAttributeName(cmd.Name); verr != nil {
		return sharedkernel.FailOf[AttributeDto](verr)
	}

	createResult := attributes.NewAttribute(cmd.Name)
	if createResult.IsFailure() {
		return sharedkernel.FailOf[AttributeDto](createResult.Err())
	}
	attribute := createResult.Value()

	existing, err := h.attributes.GetByKey(ctx, tenantID, attribute.Key)
	if err != nil {
		return sharedkernel.FailOf[AttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if existing != nil {
		return sharedkernel.FailOf[AttributeDto](sharedkernel.NewConflictError("Attribute key already exists."))
	}

	if err := h.attributes.Add(ctx, tenantID, attribute); err != nil {
		return sharedkernel.FailOf[AttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(attributeToDto(attribute))
}

// List, özellikleri anahtara göre sıralı sayfalar halinde döner.
func (h *AttributeHandlers) List(ctx context.Context, tenantID uuid.UUID, page, pageSize int) sharedkernel.ResultOf[sharedkernel.PagedResult[AttributeDto]] {
	pr := sharedkernel.ResolvePagination(page, pageSize)
	if pr.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[AttributeDto]](pr.Err())
	}
	pageResult, err := h.attributes.List(ctx, tenantID, pr.Value())
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[AttributeDto]](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(sharedkernel.MapPagedResult(pageResult, attributeToDto))
}

// Get, tek özelliği döner; yoksa not_found.
func (h *AttributeHandlers) Get(ctx context.Context, tenantID, id uuid.UUID) sharedkernel.ResultOf[AttributeDto] {
	attribute, err := h.attributes.GetByID(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.FailOf[AttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if attribute == nil {
		return sharedkernel.FailOf[AttributeDto](sharedkernel.NewNotFoundError("Attribute not found."))
	}
	return sharedkernel.OkOf(attributeToDto(attribute))
}

// Update, özelliğin görünen adını günceller (anahtar değişmez).
func (h *AttributeHandlers) Update(ctx context.Context, tenantID uuid.UUID, cmd UpdateAttributeCommand) sharedkernel.ResultOf[AttributeDto] {
	if verr := validateAttributeName(cmd.Name); verr != nil {
		return sharedkernel.FailOf[AttributeDto](verr)
	}

	attribute, err := h.attributes.GetByID(ctx, tenantID, cmd.ID)
	if err != nil {
		return sharedkernel.FailOf[AttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if attribute == nil {
		return sharedkernel.FailOf[AttributeDto](sharedkernel.NewNotFoundError("Attribute not found."))
	}

	if renameResult := attribute.Rename(cmd.Name); renameResult.IsFailure() {
		return sharedkernel.FailOf[AttributeDto](renameResult.Err())
	}
	if err := h.attributes.Update(ctx, tenantID, attribute); err != nil {
		return sharedkernel.FailOf[AttributeDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(attributeToDto(attribute))
}

// Delete, özelliği siler; yoksa not_found.
func (h *AttributeHandlers) Delete(ctx context.Context, tenantID, id uuid.UUID) sharedkernel.Result {
	attribute, err := h.attributes.GetByID(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if attribute == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Attribute not found."))
	}
	if err := h.attributes.Remove(ctx, tenantID, id); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}

// AddValue, özelliğe yeni değer ekler; ad özellik içinde benzersiz olmalıdır.
func (h *AttributeHandlers) AddValue(ctx context.Context, tenantID uuid.UUID, cmd AddAttributeValueCommand) sharedkernel.ResultOf[AttributeDefinitionValueDto] {
	if verr := validateAttributeValueInput(cmd.AttributeID, cmd.Name); verr != nil {
		return sharedkernel.FailOf[AttributeDefinitionValueDto](verr)
	}

	attribute, err := h.attributes.GetByID(ctx, tenantID, cmd.AttributeID)
	if err != nil {
		return sharedkernel.FailOf[AttributeDefinitionValueDto](sharedkernel.NewInternalError(err.Error()))
	}
	if attribute == nil {
		return sharedkernel.FailOf[AttributeDefinitionValueDto](sharedkernel.NewNotFoundError("Attribute not found."))
	}

	addResult := attribute.AddValue(cmd.Name)
	if addResult.IsFailure() {
		return sharedkernel.FailOf[AttributeDefinitionValueDto](addResult.Err())
	}
	if err := h.attributes.Update(ctx, tenantID, attribute); err != nil {
		return sharedkernel.FailOf[AttributeDefinitionValueDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(valueToDto(addResult.Value(), attribute.ID))
}

// ListValues, özelliğin değerlerini sayfalanmış döner.
func (h *AttributeHandlers) ListValues(ctx context.Context, tenantID, attributeID uuid.UUID, page, pageSize int) sharedkernel.ResultOf[sharedkernel.PagedResult[AttributeDefinitionValueDto]] {
	pr := sharedkernel.ResolvePagination(page, pageSize)
	if pr.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[AttributeDefinitionValueDto]](pr.Err())
	}

	attribute, err := h.attributes.GetByID(ctx, tenantID, attributeID)
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[AttributeDefinitionValueDto]](sharedkernel.NewInternalError(err.Error()))
	}
	if attribute == nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[AttributeDefinitionValueDto]](sharedkernel.NewNotFoundError("Attribute not found."))
	}

	rows := make([]AttributeDefinitionValueDto, 0, len(attribute.Values))
	for _, v := range attribute.Values {
		rows = append(rows, valueToDto(v, attribute.ID))
	}
	p := pr.Value()
	total := len(rows)
	start := min(p.Skip(), total)
	end := min(start+p.PageSize, total)
	return sharedkernel.OkOf(sharedkernel.NewPagedResult(rows[start:end], p, total))
}

// UpdateValue, değer adını günceller; değerin sahibi özellik değer kimliğiyle bulunur.
func (h *AttributeHandlers) UpdateValue(ctx context.Context, tenantID uuid.UUID, cmd UpdateAttributeValueCommand) sharedkernel.ResultOf[AttributeDefinitionValueDto] {
	if verr := validateAttributeValueName(cmd.Name); verr != nil {
		return sharedkernel.FailOf[AttributeDefinitionValueDto](verr)
	}

	owner, err := h.attributes.FindByValueID(ctx, tenantID, cmd.ID)
	if err != nil {
		return sharedkernel.FailOf[AttributeDefinitionValueDto](sharedkernel.NewInternalError(err.Error()))
	}
	if owner == nil {
		return sharedkernel.FailOf[AttributeDefinitionValueDto](sharedkernel.NewNotFoundError("Attribute value not found."))
	}

	if updateResult := owner.UpdateValue(cmd.ID, cmd.Name); updateResult.IsFailure() {
		return sharedkernel.FailOf[AttributeDefinitionValueDto](updateResult.Err())
	}
	if err := h.attributes.Update(ctx, tenantID, owner); err != nil {
		return sharedkernel.FailOf[AttributeDefinitionValueDto](sharedkernel.NewInternalError(err.Error()))
	}

	for _, v := range owner.Values {
		if v.ID == cmd.ID {
			return sharedkernel.OkOf(valueToDto(v, owner.ID))
		}
	}
	return sharedkernel.FailOf[AttributeDefinitionValueDto](sharedkernel.NewNotFoundError("Attribute value not found."))
}

// RemoveValue, değeri özellikten kaldırır.
func (h *AttributeHandlers) RemoveValue(ctx context.Context, tenantID, valueID uuid.UUID) sharedkernel.Result {
	owner, err := h.attributes.FindByValueID(ctx, tenantID, valueID)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if owner == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Attribute value not found."))
	}
	if removeResult := owner.RemoveValue(valueID); removeResult.IsFailure() {
		return sharedkernel.Fail(removeResult.Err())
	}
	if err := h.attributes.Update(ctx, tenantID, owner); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}
