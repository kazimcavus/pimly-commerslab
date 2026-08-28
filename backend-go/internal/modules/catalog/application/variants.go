package application

import (
	"context"
	"net/url"
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/variants"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Varyant alanı uzunluk sınırları (.NET CatalogValidationRules sabitleri).
const (
	VariantTypeNameMaxLength     = 200
	VariantValueLabelMaxLength   = 200
	VariantValueColorMaxLength   = 50
	VariantValueImageUrlMaxLength = 2000
	VariantValueKeyMaxLength     = 200
)

// VariantTypeDto, varyant tipi veri transfer nesnesidir.
type VariantTypeDto struct {
	ID             uuid.UUID `json:"id"`
	Key            string    `json:"key"`
	Name           string    `json:"name"`
	SelectionStyle string    `json:"selection_style"`
	SortOrder      int       `json:"sort_order"`
	Slicer         bool      `json:"slicer"`
}

// VariantValueDto, varyant değeri veri transfer nesnesidir.
type VariantValueDto struct {
	ID            uuid.UUID `json:"id"`
	VariantTypeID uuid.UUID `json:"variant_type_id"`
	Key           string    `json:"key"`
	Label         string    `json:"label"`
	Color         *string   `json:"color"`
	ImageURL      *string   `json:"image_url"`
	SortOrder     int       `json:"sort_order"`
}

// variantToDto, domain varyant türünü DTO'ya çevirir.
func variantToDto(v *variants.Variant) VariantTypeDto {
	return VariantTypeDto{
		ID:             v.ID,
		Key:            v.Key,
		Name:           v.Name,
		SelectionStyle: string(v.SelectionStyle),
		SortOrder:      v.SortOrder,
		Slicer:         v.Slicer,
	}
}

// variantValueToDto, varyant değerini DTO'ya çevirir.
func variantValueToDto(v *variants.Value, variantTypeID uuid.UUID) VariantValueDto {
	return VariantValueDto{
		ID:            v.ID,
		VariantTypeID: variantTypeID,
		Key:           v.Key,
		Label:         v.Label,
		Color:         v.Color,
		ImageURL:      v.ImageURL,
		SortOrder:     v.SortOrder,
	}
}

// VariantRepository, varyant türü kalıcılık portudur (.NET IVariantRepository
// karşılığı). Tüm metodlar tenant kimliğini açıkça alır.
type VariantRepository interface {
	// GetByID, türü değerleriyle birlikte döner; yoksa nil.
	GetByID(ctx context.Context, tenantID, id uuid.UUID) (*variants.Variant, error)

	// GetByName, türü ada göre (birebir eşleşme) döner; yoksa nil.
	GetByName(ctx context.Context, tenantID uuid.UUID, name string) (*variants.Variant, error)

	// GetByKey, türü anahtarına göre döner; yoksa nil.
	GetByKey(ctx context.Context, tenantID uuid.UUID, key string) (*variants.Variant, error)

	// GetSlicerVariant, slicer işaretli türü döner (excludeID hariç); yoksa nil.
	GetSlicerVariant(ctx context.Context, tenantID uuid.UUID, excludeID *uuid.UUID) (*variants.Variant, error)

	// List, türleri sıra + ada göre sıralı ve sayfalanmış listeler.
	List(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[*variants.Variant], error)

	// FindByValueID, değer kimliğinin ait olduğu türü (değerleriyle) döner; yoksa nil.
	FindByValueID(ctx context.Context, tenantID, valueID uuid.UUID) (*variants.Variant, error)

	// Add, yeni türü (varsa değerleriyle) ekler.
	Add(ctx context.Context, tenantID uuid.UUID, variant *variants.Variant) error

	// Update, tür alanlarını ve değer koleksiyonunu kalıcılaştırır.
	Update(ctx context.Context, tenantID uuid.UUID, variant *variants.Variant) error

	// Remove, türü siler (değerler veritabanında cascade silinir).
	Remove(ctx context.Context, tenantID, id uuid.UUID) error
}

// CreateVariantTypeCommand, yeni varyant türü isteğini taşır; SelectionStyle
// boşsa "list" varsayılır, Key boşsa adından türetilir.
type CreateVariantTypeCommand struct {
	Name           string
	SelectionStyle string
	SortOrder      int
	Slicer         bool
	Key            *string
}

// UpdateVariantTypeCommand, tür güncelleme isteğini taşır (anahtar değişmez).
type UpdateVariantTypeCommand struct {
	ID             uuid.UUID
	Name           string
	SelectionStyle string
	SortOrder      int
	Slicer         bool
}

// VariantValueCommand, değer ekleme/güncelleme isteğini taşır.
type VariantValueCommand struct {
	VariantTypeID uuid.UUID // ekleme yolunda tür kimliği; güncellemede kullanılmaz
	ValueID       uuid.UUID // güncelleme/silme yolunda değer kimliği
	Label         string
	Color         *string
	ImageURL      *string
	Key           *string
	SortOrder     int
}

// isAllowedMediaURL, görsel URL'sinin bu tenant'ın medya deposuna işaret edip
// etmediğini denetler (.NET IsAllowedMediaUrl portu): /media/{tenant:N}/ öneki,
// göreli veya mutlak URL'nin yol kısmında aranır.
func isAllowedMediaURL(rawURL, allowedPrefix string, tenantID uuid.UUID) bool {
	trimmed := strings.TrimSpace(rawURL)
	if trimmed == "" {
		return false
	}
	tenantPrefix := strings.TrimRight(allowedPrefix, "/") + "/" + strings.ReplaceAll(tenantID.String(), "-", "") + "/"
	if hasPrefixFold(trimmed, tenantPrefix) {
		return true
	}
	if parsed, err := url.Parse(trimmed); err == nil && parsed.IsAbs() {
		return hasPrefixFold(parsed.Path, tenantPrefix)
	}
	return false
}

// hasPrefixFold, büyük/küçük harf duyarsız önek denetimidir.
func hasPrefixFold(s, prefix string) bool {
	return len(s) >= len(prefix) && strings.EqualFold(s[:len(prefix)], prefix)
}

// validateVariantTypeInput, tür oluşturma/güncelleme kurallarını uygular
// (.NET Create/UpdateVariantTypeCommandValidator portu).
func validateVariantTypeInput(name, selectionStyle string, key *string) *sharedkernel.Error {
	var f fieldErrors
	f.required("name", "Name", name)
	f.maxLength("name", "Name", name, VariantTypeNameMaxLength)
	if strings.TrimSpace(selectionStyle) != "" {
		if _, ok := variants.ParseSelectionStyle(selectionStyle); !ok {
			f.errs = append(f.errs, sharedkernel.ValidationError{
				Field: "selection_style", Code: sharedkernel.ValidationCodeInvalidEnum,
				Message: "SelectionStyle has an invalid value."})
		}
	}
	f.maxLength("key", "Key", deref(key), VariantValueKeyMaxLength)
	return f.failure()
}

// validateVariantValueInput, değer kurallarını uygular
// (.NET Add/UpdateVariantValueCommandValidator portu). requireTypeID, ekleme
// yolunda VariantTypeId'nin doğrulanmasını açar.
func validateVariantValueInput(cmd VariantValueCommand, allowedPrefix string, tenantID uuid.UUID, requireTypeID bool) *sharedkernel.Error {
	var f fieldErrors
	if requireTypeID {
		f.requiredID("variant_type_id", "VariantTypeId", cmd.VariantTypeID)
	}
	f.required("label", "Label", cmd.Label)
	f.maxLength("label", "Label", cmd.Label, VariantValueLabelMaxLength)
	f.maxLength("color", "Color", deref(cmd.Color), VariantValueColorMaxLength)
	imageURL := deref(cmd.ImageURL)
	f.maxLength("image_url", "ImageUrl", imageURL, VariantValueImageUrlMaxLength)
	if strings.TrimSpace(imageURL) != "" && !isAllowedMediaURL(imageURL, allowedPrefix, tenantID) {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: "image_url", Code: sharedkernel.ValidationCodeInvalidFormat,
			Message: "ImageUrl must reference an uploaded media asset."})
	}
	f.maxLength("key", "Key", deref(cmd.Key), VariantValueKeyMaxLength)
	return f.failure()
}

// VariantHandlers, varyant türü ve değeri kullanım senaryolarını yürütür
// (.NET'teki dokuz ayrı handler sınıfının Go karşılığı).
type VariantHandlers struct {
	variants         VariantRepository
	allowedURLPrefix string
}

// NewVariantHandlers, bağımlılıklarıyla varyant handler'larını oluşturur;
// allowedURLPrefix, Media:AllowedUrlPrefix yapılandırmasıdır (görsel URL denetimi).
func NewVariantHandlers(variants VariantRepository, allowedURLPrefix string) *VariantHandlers {
	return &VariantHandlers{variants: variants, allowedURLPrefix: allowedURLPrefix}
}

// Create, yeni varyant türü oluşturur; ad/anahtar çakışmaları ve ikinci slicer
// conflict döner.
func (h *VariantHandlers) Create(ctx context.Context, tenantID uuid.UUID, cmd CreateVariantTypeCommand) sharedkernel.ResultOf[VariantTypeDto] {
	if verr := validateVariantTypeInput(cmd.Name, cmd.SelectionStyle, cmd.Key); verr != nil {
		return sharedkernel.FailOf[VariantTypeDto](verr)
	}

	existing, err := h.variants.GetByName(ctx, tenantID, strings.TrimSpace(cmd.Name))
	if err != nil {
		return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if existing != nil {
		return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewConflictError("Variant type name already exists."))
	}

	if cmd.Slicer {
		slicer, err := h.variants.GetSlicerVariant(ctx, tenantID, nil)
		if err != nil {
			return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewInternalError(err.Error()))
		}
		if slicer != nil {
			return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewConflictError("Only one variant type can be marked as slicer."))
		}
	}

	style := variants.StyleList
	if strings.TrimSpace(cmd.SelectionStyle) != "" {
		style, _ = variants.ParseSelectionStyle(cmd.SelectionStyle)
	}

	createResult := variants.NewVariant(cmd.Name, style, cmd.SortOrder, cmd.Slicer, cmd.Key)
	if createResult.IsFailure() {
		return sharedkernel.FailOf[VariantTypeDto](createResult.Err())
	}
	variant := createResult.Value()

	byKey, err := h.variants.GetByKey(ctx, tenantID, variant.Key)
	if err != nil {
		return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if byKey != nil {
		return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewConflictError("Variant key already exists."))
	}

	if err := h.variants.Add(ctx, tenantID, variant); err != nil {
		return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(variantToDto(variant))
}

// List, türleri sıra + ada göre sayfalanmış döner.
func (h *VariantHandlers) List(ctx context.Context, tenantID uuid.UUID, page, pageSize int) sharedkernel.ResultOf[sharedkernel.PagedResult[VariantTypeDto]] {
	pr := sharedkernel.ResolvePagination(page, pageSize)
	if pr.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[VariantTypeDto]](pr.Err())
	}
	pageResult, err := h.variants.List(ctx, tenantID, pr.Value())
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[VariantTypeDto]](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(sharedkernel.MapPagedResult(pageResult, variantToDto))
}

// Get, tek türü döner; yoksa not_found.
func (h *VariantHandlers) Get(ctx context.Context, tenantID, id uuid.UUID) sharedkernel.ResultOf[VariantTypeDto] {
	variant, err := h.variants.GetByID(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if variant == nil {
		return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewNotFoundError("Variant type not found."))
	}
	return sharedkernel.OkOf(variantToDto(variant))
}

// Update, türün ad/stil/sıra/slicer alanlarını günceller.
func (h *VariantHandlers) Update(ctx context.Context, tenantID uuid.UUID, cmd UpdateVariantTypeCommand) sharedkernel.ResultOf[VariantTypeDto] {
	if verr := validateVariantTypeInput(cmd.Name, cmd.SelectionStyle, nil); verr != nil {
		return sharedkernel.FailOf[VariantTypeDto](verr)
	}

	variant, err := h.variants.GetByID(ctx, tenantID, cmd.ID)
	if err != nil {
		return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if variant == nil {
		return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewNotFoundError("Variant type not found."))
	}

	existing, err := h.variants.GetByName(ctx, tenantID, strings.TrimSpace(cmd.Name))
	if err != nil {
		return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewInternalError(err.Error()))
	}
	if existing != nil && existing.ID != cmd.ID {
		return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewConflictError("Variant type name already exists."))
	}

	if cmd.Slicer {
		slicer, err := h.variants.GetSlicerVariant(ctx, tenantID, &cmd.ID)
		if err != nil {
			return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewInternalError(err.Error()))
		}
		if slicer != nil {
			return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewConflictError("Only one variant type can be marked as slicer."))
		}
	}

	style := variants.StyleList
	if strings.TrimSpace(cmd.SelectionStyle) != "" {
		style, _ = variants.ParseSelectionStyle(cmd.SelectionStyle)
	}
	if renameResult := variant.Rename(cmd.Name, style, cmd.SortOrder, cmd.Slicer); renameResult.IsFailure() {
		return sharedkernel.FailOf[VariantTypeDto](renameResult.Err())
	}
	if err := h.variants.Update(ctx, tenantID, variant); err != nil {
		return sharedkernel.FailOf[VariantTypeDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(variantToDto(variant))
}

// Delete, türü siler; yoksa not_found.
func (h *VariantHandlers) Delete(ctx context.Context, tenantID, id uuid.UUID) sharedkernel.Result {
	variant, err := h.variants.GetByID(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if variant == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Variant type not found."))
	}
	if err := h.variants.Remove(ctx, tenantID, id); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}

// AddValue, türe yeni değer ekler.
func (h *VariantHandlers) AddValue(ctx context.Context, tenantID uuid.UUID, cmd VariantValueCommand) sharedkernel.ResultOf[VariantValueDto] {
	if verr := validateVariantValueInput(cmd, h.allowedURLPrefix, tenantID, true); verr != nil {
		return sharedkernel.FailOf[VariantValueDto](verr)
	}

	variant, err := h.variants.GetByID(ctx, tenantID, cmd.VariantTypeID)
	if err != nil {
		return sharedkernel.FailOf[VariantValueDto](sharedkernel.NewInternalError(err.Error()))
	}
	if variant == nil {
		return sharedkernel.FailOf[VariantValueDto](sharedkernel.NewNotFoundError("Variant type not found."))
	}

	addResult := variant.AddValue(cmd.Label, cmd.Color, cmd.ImageURL, cmd.Key, cmd.SortOrder)
	if addResult.IsFailure() {
		return sharedkernel.FailOf[VariantValueDto](addResult.Err())
	}
	if err := h.variants.Update(ctx, tenantID, variant); err != nil {
		return sharedkernel.FailOf[VariantValueDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(variantValueToDto(addResult.Value(), variant.ID))
}

// ListValues, türün değerlerini sayfalanmış döner.
func (h *VariantHandlers) ListValues(ctx context.Context, tenantID, variantTypeID uuid.UUID, page, pageSize int) sharedkernel.ResultOf[sharedkernel.PagedResult[VariantValueDto]] {
	pr := sharedkernel.ResolvePagination(page, pageSize)
	if pr.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[VariantValueDto]](pr.Err())
	}

	variant, err := h.variants.GetByID(ctx, tenantID, variantTypeID)
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[VariantValueDto]](sharedkernel.NewInternalError(err.Error()))
	}
	if variant == nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[VariantValueDto]](sharedkernel.NewNotFoundError("Variant type not found."))
	}

	rows := make([]VariantValueDto, 0, len(variant.Values))
	for _, v := range variant.Values {
		rows = append(rows, variantValueToDto(v, variant.ID))
	}
	p := pr.Value()
	total := len(rows)
	start := min(p.Skip(), total)
	end := min(start+p.PageSize, total)
	return sharedkernel.OkOf(sharedkernel.NewPagedResult(rows[start:end], p, total))
}

// UpdateValue, değer bilgilerini günceller; değerin sahibi tür değer kimliğiyle bulunur.
func (h *VariantHandlers) UpdateValue(ctx context.Context, tenantID uuid.UUID, cmd VariantValueCommand) sharedkernel.ResultOf[VariantValueDto] {
	if verr := validateVariantValueInput(cmd, h.allowedURLPrefix, tenantID, false); verr != nil {
		return sharedkernel.FailOf[VariantValueDto](verr)
	}

	owner, err := h.variants.FindByValueID(ctx, tenantID, cmd.ValueID)
	if err != nil {
		return sharedkernel.FailOf[VariantValueDto](sharedkernel.NewInternalError(err.Error()))
	}
	if owner == nil {
		return sharedkernel.FailOf[VariantValueDto](sharedkernel.NewNotFoundError("Variant value not found."))
	}

	if updateResult := owner.UpdateValue(cmd.ValueID, cmd.Label, cmd.Color, cmd.ImageURL, cmd.Key, cmd.SortOrder); updateResult.IsFailure() {
		return sharedkernel.FailOf[VariantValueDto](updateResult.Err())
	}
	if err := h.variants.Update(ctx, tenantID, owner); err != nil {
		return sharedkernel.FailOf[VariantValueDto](sharedkernel.NewInternalError(err.Error()))
	}
	for _, v := range owner.Values {
		if v.ID == cmd.ValueID {
			return sharedkernel.OkOf(variantValueToDto(v, owner.ID))
		}
	}
	return sharedkernel.FailOf[VariantValueDto](sharedkernel.NewNotFoundError("Variant value not found."))
}

// RemoveValue, değeri türden kaldırır.
func (h *VariantHandlers) RemoveValue(ctx context.Context, tenantID, valueID uuid.UUID) sharedkernel.Result {
	owner, err := h.variants.FindByValueID(ctx, tenantID, valueID)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if owner == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Variant value not found."))
	}
	if removeResult := owner.RemoveValue(valueID); removeResult.IsFailure() {
		return sharedkernel.Fail(removeResult.Err())
	}
	if err := h.variants.Update(ctx, tenantID, owner); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}
