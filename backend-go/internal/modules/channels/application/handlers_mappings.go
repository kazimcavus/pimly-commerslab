package application

import (
	"context"
	"fmt"
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// enrichCategoryMapping, eşlemeyi Catalog ve harici kategori özetleriyle
// zenginleştirir (.NET CategoryChannelMappingEnricher karşılığı).
func (h *Handlers) enrichCategoryMapping(ctx context.Context, tenantID uuid.UUID, mapping *domain.CategoryChannelMapping) (CategoryChannelMappingDto, error) {
	catalogCategory, err := h.catalog.GetCategorySnapshot(ctx, tenantID, mapping.CatalogCategoryID)
	if err != nil {
		return CategoryChannelMappingDto{}, err
	}
	externalCategory, err := h.repo.GetExternalCategory(ctx, mapping.MarketplaceCode, mapping.ExternalID)
	if err != nil {
		return CategoryChannelMappingDto{}, err
	}
	dto := CategoryChannelMappingDto{
		ID: mapping.ID, CatalogCategoryID: mapping.CatalogCategoryID,
		MarketplaceCode: mapping.MarketplaceCode, ExternalID: mapping.ExternalID,
		CatalogCategory: catalogCategory,
	}
	if externalCategory != nil {
		dto.ExternalCategory = &ExternalCategorySummaryDto{
			ExternalID: externalCategory.ExternalID, Name: externalCategory.Name,
			Path: externalCategory.Path, IsLeaf: externalCategory.IsLeaf, SyncedAt: externalCategory.SyncedAt,
		}
	}
	return dto, nil
}

// UpsertCategoryMapping, Catalog kategorisini pazaryeri kategorisine eşler;
// yalnızca yaprak harici kategoriler eşlenebilir.
func (h *Handlers) UpsertCategoryMapping(ctx context.Context, tenantID uuid.UUID, code string, catalogCategoryID uuid.UUID, externalID string) sharedkernel.ResultOf[CategoryChannelMappingDto] {
	if strings.TrimSpace(externalID) == "" {
		return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewValidationError("External category id is required."))
	}
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[CategoryChannelMappingDto](marketplace.Err())
	}
	mp := marketplace.Value().Code()

	catalogCategory, err := h.catalog.GetCategorySnapshot(ctx, tenantID, catalogCategoryID)
	if err != nil {
		return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	if catalogCategory == nil {
		return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewNotFoundError("Catalog category not found."))
	}

	externalCategory, err := h.repo.GetExternalCategory(ctx, mp, strings.TrimSpace(externalID))
	if err != nil {
		return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	if externalCategory == nil {
		return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewNotFoundError("External category not found."))
	}
	if !externalCategory.IsLeaf {
		return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewValidationError(
			"Only leaf external categories can be mapped."))
	}

	existing, err := h.repo.GetCategoryMapping(ctx, tenantID, mp, catalogCategoryID)
	if err != nil {
		return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	if existing == nil {
		existing = &domain.CategoryChannelMapping{
			ID: uuid.New(), CatalogCategoryID: catalogCategoryID,
			MarketplaceCode: mp, ExternalID: strings.TrimSpace(externalID),
		}
		if err := h.repo.AddCategoryMapping(ctx, tenantID, existing); err != nil {
			return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
		}
	} else {
		existing.ExternalID = strings.TrimSpace(externalID)
		if err := h.repo.UpdateCategoryMapping(ctx, tenantID, existing); err != nil {
			return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
		}
	}

	dto, err := h.enrichCategoryMapping(ctx, tenantID, existing)
	if err != nil {
		return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(dto)
}

// GetCategoryMapping, kategori eşlemesini döner; yoksa not_found.
func (h *Handlers) GetCategoryMapping(ctx context.Context, tenantID uuid.UUID, code string, catalogCategoryID uuid.UUID) sharedkernel.ResultOf[CategoryChannelMappingDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[CategoryChannelMappingDto](marketplace.Err())
	}
	mapping, err := h.repo.GetCategoryMapping(ctx, tenantID, marketplace.Value().Code(), catalogCategoryID)
	if err != nil {
		return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	if mapping == nil {
		return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewNotFoundError("Category channel mapping not found."))
	}
	dto, err := h.enrichCategoryMapping(ctx, tenantID, mapping)
	if err != nil {
		return sharedkernel.FailOf[CategoryChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(dto)
}

// ListCategoryMappings, kategori eşlemelerini sayfalanmış döner.
func (h *Handlers) ListCategoryMappings(ctx context.Context, tenantID uuid.UUID, code string, catalogCategoryID *uuid.UUID, page, pageSize int) sharedkernel.ResultOf[sharedkernel.PagedResult[CategoryChannelMappingDto]] {
	pr := sharedkernel.ResolvePagination(page, pageSize)
	if pr.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[CategoryChannelMappingDto]](pr.Err())
	}
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[CategoryChannelMappingDto]](marketplace.Err())
	}

	mappings, total, err := h.repo.ListCategoryMappings(ctx, tenantID, marketplace.Value().Code(), catalogCategoryID, pr.Value())
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[CategoryChannelMappingDto]](sharedkernel.NewInternalError(err.Error()))
	}
	items := make([]CategoryChannelMappingDto, len(mappings))
	for i, mapping := range mappings {
		dto, err := h.enrichCategoryMapping(ctx, tenantID, mapping)
		if err != nil {
			return sharedkernel.FailOf[sharedkernel.PagedResult[CategoryChannelMappingDto]](sharedkernel.NewInternalError(err.Error()))
		}
		items[i] = dto
	}
	return sharedkernel.OkOf(sharedkernel.PagedResult[CategoryChannelMappingDto]{
		Items: items, Page: pr.Value().Page, PageSize: pr.Value().PageSize,
		TotalCount: total, TotalPages: totalPages(total, pr.Value().PageSize),
	})
}

// totalPages, toplam sayfa sayısını yukarı yuvarlayarak hesaplar.
func totalPages(total, pageSize int) int {
	if pageSize == 0 {
		return 0
	}
	return (total + pageSize - 1) / pageSize
}

// DeleteCategoryMapping, kategori eşlemesini kaldırır.
func (h *Handlers) DeleteCategoryMapping(ctx context.Context, tenantID uuid.UUID, code string, catalogCategoryID uuid.UUID) sharedkernel.Result {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.Fail(marketplace.Err())
	}
	mapping, err := h.repo.GetCategoryMapping(ctx, tenantID, marketplace.Value().Code(), catalogCategoryID)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if mapping == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Category channel mapping not found."))
	}
	if err := h.repo.RemoveCategoryMapping(ctx, tenantID, mapping.ID); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}

// enrichAttributeMapping, alan eşlemesini Catalog kaynak ve harici özellik
// özetleriyle zenginleştirir (.NET AttributeChannelMappingEnricher karşılığı).
func (h *Handlers) enrichAttributeMapping(ctx context.Context, tenantID uuid.UUID, mapping *domain.AttributeChannelMapping) (AttributeChannelMappingDto, error) {
	dto := AttributeChannelMappingDto{
		ID: mapping.ID, CatalogCategoryID: mapping.CatalogCategoryID,
		MarketplaceCode: mapping.MarketplaceCode, SourceType: string(mapping.SourceType),
		CatalogSourceID: mapping.CatalogSourceID, ExternalAttributeID: mapping.ExternalAttributeID,
	}

	externalCategoryID, err := h.repo.ResolveExternalCategoryID(ctx, tenantID, mapping.MarketplaceCode, mapping.CatalogCategoryID)
	if err != nil {
		return dto, err
	}
	if externalCategoryID != nil {
		externalAttribute, err := h.repo.GetExternalAttribute(ctx, mapping.MarketplaceCode, *externalCategoryID, mapping.ExternalAttributeID)
		if err != nil {
			return dto, err
		}
		if externalAttribute != nil {
			dto.ExternalAttribute = &ExternalCategoryAttributeSummaryDto{
				ExternalAttributeID: externalAttribute.ExternalAttributeID, Name: externalAttribute.Name,
				Required: externalAttribute.Required, AllowCustom: externalAttribute.AllowCustom,
				IsVariant: externalAttribute.IsVariant, IsSlicer: externalAttribute.IsSlicer,
			}
		}
	}

	if mapping.SourceType == domain.SourceCatalogAttribute {
		dto.CatalogAttribute, err = h.catalog.GetAttributeSnapshot(ctx, tenantID, mapping.CatalogSourceID)
	} else {
		dto.CatalogVariant, err = h.catalog.GetVariantSnapshot(ctx, tenantID, mapping.CatalogSourceID)
	}
	return dto, err
}

// UpsertAttributeMapping, Catalog özelliğini/varyantını harici özelliğe eşler.
func (h *Handlers) UpsertAttributeMapping(ctx context.Context, tenantID uuid.UUID, code string, catalogCategoryID uuid.UUID, sourceType string, catalogSourceID uuid.UUID, externalAttributeID string) sharedkernel.ResultOf[AttributeChannelMappingDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[AttributeChannelMappingDto](marketplace.Err())
	}
	mp := marketplace.Value().Code()

	parsedSourceType := domain.ParseSourceType(sourceType)
	if parsedSourceType.IsFailure() {
		return sharedkernel.FailOf[AttributeChannelMappingDto](parsedSourceType.Err())
	}
	if strings.TrimSpace(externalAttributeID) == "" {
		return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewValidationError("External attribute id is required."))
	}

	externalCategoryID, err := h.repo.ResolveExternalCategoryID(ctx, tenantID, mp, catalogCategoryID)
	if err != nil {
		return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	if externalCategoryID == nil {
		return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewNotFoundError(
			"Category channel mapping required before attribute mapping."))
	}

	if parsedSourceType.Value() == domain.SourceCatalogAttribute {
		snapshot, err := h.catalog.GetAttributeSnapshot(ctx, tenantID, catalogSourceID)
		if err != nil {
			return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
		}
		if snapshot == nil {
			return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewNotFoundError("Catalog attribute not found."))
		}
		belongs, err := h.catalog.AttributeBelongsToCategory(ctx, tenantID, catalogCategoryID, catalogSourceID)
		if err != nil {
			return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
		}
		if !belongs {
			return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewNotFoundError(
				"Catalog attribute is not assigned to the category."))
		}
	} else {
		snapshot, err := h.catalog.GetVariantSnapshot(ctx, tenantID, catalogSourceID)
		if err != nil {
			return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
		}
		if snapshot == nil {
			return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewNotFoundError("Catalog variant not found."))
		}
	}

	externalAttribute, err := h.repo.GetExternalAttribute(ctx, mp, *externalCategoryID, strings.TrimSpace(externalAttributeID))
	if err != nil {
		return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	if externalAttribute == nil {
		return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewNotFoundError("External attribute not found."))
	}

	existing, err := h.repo.GetAttributeMapping(ctx, tenantID, mp, catalogCategoryID, parsedSourceType.Value(), catalogSourceID)
	if err != nil {
		return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	if existing == nil {
		existing = &domain.AttributeChannelMapping{
			ID: uuid.New(), MarketplaceCode: mp, CatalogCategoryID: catalogCategoryID,
			SourceType: parsedSourceType.Value(), CatalogSourceID: catalogSourceID,
			ExternalAttributeID: strings.TrimSpace(externalAttributeID),
		}
		if err := h.repo.AddAttributeMapping(ctx, tenantID, existing); err != nil {
			return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
		}
	} else {
		existing.ExternalAttributeID = strings.TrimSpace(externalAttributeID)
		if err := h.repo.UpdateAttributeMapping(ctx, tenantID, existing); err != nil {
			return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
		}
	}

	dto, err := h.enrichAttributeMapping(ctx, tenantID, existing)
	if err != nil {
		return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(dto)
}

// ListAttributeMappings, kategori altındaki alan eşlemelerini sayfalanmış döner.
func (h *Handlers) ListAttributeMappings(ctx context.Context, tenantID uuid.UUID, code string, catalogCategoryID uuid.UUID, sourceType *string, page, pageSize int) sharedkernel.ResultOf[sharedkernel.PagedResult[AttributeChannelMappingDto]] {
	pr := sharedkernel.ResolvePagination(page, pageSize)
	if pr.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[AttributeChannelMappingDto]](pr.Err())
	}
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[AttributeChannelMappingDto]](marketplace.Err())
	}

	var parsedSourceType *domain.AttributeMappingSourceType
	if sourceType != nil && strings.TrimSpace(*sourceType) != "" {
		parsed := domain.ParseSourceType(*sourceType)
		if parsed.IsFailure() {
			return sharedkernel.FailOf[sharedkernel.PagedResult[AttributeChannelMappingDto]](parsed.Err())
		}
		value := parsed.Value()
		parsedSourceType = &value
	}

	mappings, total, err := h.repo.ListAttributeMappings(ctx, tenantID, marketplace.Value().Code(), catalogCategoryID, parsedSourceType, pr.Value())
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[AttributeChannelMappingDto]](sharedkernel.NewInternalError(err.Error()))
	}
	items := make([]AttributeChannelMappingDto, len(mappings))
	for i, mapping := range mappings {
		dto, err := h.enrichAttributeMapping(ctx, tenantID, mapping)
		if err != nil {
			return sharedkernel.FailOf[sharedkernel.PagedResult[AttributeChannelMappingDto]](sharedkernel.NewInternalError(err.Error()))
		}
		items[i] = dto
	}
	return sharedkernel.OkOf(sharedkernel.PagedResult[AttributeChannelMappingDto]{
		Items: items, Page: pr.Value().Page, PageSize: pr.Value().PageSize,
		TotalCount: total, TotalPages: totalPages(total, pr.Value().PageSize),
	})
}

// getOwnedAttributeMapping, kimlikle alan eşlemesini döner ve pazaryeri +
// kategori sahipliğini doğrular; uymuyorsa nil.
func (h *Handlers) getOwnedAttributeMapping(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID, mappingID uuid.UUID) (*domain.AttributeChannelMapping, error) {
	mapping, err := h.repo.GetAttributeMappingByID(ctx, tenantID, mappingID)
	if err != nil {
		return nil, err
	}
	if mapping == nil || mapping.MarketplaceCode != marketplaceCode || mapping.CatalogCategoryID != catalogCategoryID {
		return nil, nil
	}
	return mapping, nil
}

// GetAttributeMapping, tek alan eşlemesini döner.
func (h *Handlers) GetAttributeMapping(ctx context.Context, tenantID uuid.UUID, code string, catalogCategoryID, mappingID uuid.UUID) sharedkernel.ResultOf[AttributeChannelMappingDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[AttributeChannelMappingDto](marketplace.Err())
	}
	mapping, err := h.getOwnedAttributeMapping(ctx, tenantID, marketplace.Value().Code(), catalogCategoryID, mappingID)
	if err != nil {
		return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	if mapping == nil {
		return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewNotFoundError("Attribute channel mapping not found."))
	}
	dto, err := h.enrichAttributeMapping(ctx, tenantID, mapping)
	if err != nil {
		return sharedkernel.FailOf[AttributeChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(dto)
}

// DeleteAttributeMapping, alan eşlemesini kaldırır.
func (h *Handlers) DeleteAttributeMapping(ctx context.Context, tenantID uuid.UUID, code string, catalogCategoryID, mappingID uuid.UUID) sharedkernel.Result {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.Fail(marketplace.Err())
	}
	mapping, err := h.getOwnedAttributeMapping(ctx, tenantID, marketplace.Value().Code(), catalogCategoryID, mappingID)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if mapping == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Attribute channel mapping not found."))
	}
	if err := h.repo.RemoveAttributeMapping(ctx, tenantID, mapping.ID); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}

// enrichValueMapping, değer eşlemesini catalog değer adı ve harici değer
// özetiyle zenginleştirir.
func (h *Handlers) enrichValueMapping(ctx context.Context, tenantID uuid.UUID, mapping *domain.AttributeValueChannelMapping, parent *domain.AttributeChannelMapping) (AttributeValueChannelMappingDto, error) {
	dto := AttributeValueChannelMappingDto{
		ID: mapping.ID, AttributeChannelMappingID: mapping.AttributeChannelMappingID,
		CatalogValueID: mapping.CatalogValueID, ExternalValueID: mapping.ExternalValueID,
	}

	var err error
	if parent.SourceType == domain.SourceCatalogAttribute {
		dto.CatalogValueName, err = h.catalog.GetAttributeValueName(ctx, tenantID, parent.CatalogSourceID, mapping.CatalogValueID)
	} else {
		dto.CatalogValueName, err = h.catalog.GetVariantValueLabel(ctx, tenantID, parent.CatalogSourceID, mapping.CatalogValueID)
	}
	if err != nil {
		return dto, err
	}

	externalCategoryID, err := h.repo.ResolveExternalCategoryID(ctx, tenantID, parent.MarketplaceCode, parent.CatalogCategoryID)
	if err != nil || externalCategoryID == nil {
		return dto, err
	}
	externalValue, err := h.repo.GetExternalValue(ctx, parent.MarketplaceCode, *externalCategoryID, parent.ExternalAttributeID, mapping.ExternalValueID)
	if err != nil {
		return dto, err
	}
	if externalValue != nil {
		dto.ExternalValue = &ExternalAttributeValueSummaryDto{
			ExternalValueID: externalValue.ExternalValueID, Name: externalValue.Name}
	}
	return dto, nil
}

// ValueMappingEntry, toplu değer eşleme girdisidir.
type ValueMappingEntry struct {
	CatalogValueID  uuid.UUID `json:"catalog_value_id"`
	ExternalValueID string    `json:"external_value_id"`
}

// UpsertValueMappings, alan eşlemesi altındaki değer eşlemelerini toplu
// oluşturur/günceller (.NET UpsertAttributeValueChannelMappingsHandler portu).
func (h *Handlers) UpsertValueMappings(ctx context.Context, tenantID uuid.UUID, code string, catalogCategoryID, mappingID uuid.UUID, entries []ValueMappingEntry) sharedkernel.ResultOf[[]AttributeValueChannelMappingDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](marketplace.Err())
	}
	mp := marketplace.Value().Code()

	parent, err := h.getOwnedAttributeMapping(ctx, tenantID, mp, catalogCategoryID, mappingID)
	if err != nil {
		return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	if parent == nil {
		return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewNotFoundError("Attribute channel mapping not found."))
	}

	externalCategoryID, err := h.repo.ResolveExternalCategoryID(ctx, tenantID, mp, catalogCategoryID)
	if err != nil {
		return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	if externalCategoryID == nil {
		return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewNotFoundError(
			"Category channel mapping required before value mapping."))
	}
	externalAttribute, err := h.repo.GetExternalAttribute(ctx, mp, *externalCategoryID, parent.ExternalAttributeID)
	if err != nil {
		return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	if externalAttribute == nil {
		return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewNotFoundError("External attribute not found."))
	}

	seen := map[uuid.UUID]struct{}{}
	for _, entry := range entries {
		if _, dup := seen[entry.CatalogValueID]; dup {
			return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewValidationError(
				"Duplicate catalog value ids are not allowed in the same batch."))
		}
		seen[entry.CatalogValueID] = struct{}{}
	}

	upserted := make([]*domain.AttributeValueChannelMapping, 0, len(entries))
	for _, entry := range entries {
		var catalogValueName *string
		if parent.SourceType == domain.SourceCatalogAttribute {
			catalogValueName, err = h.catalog.GetAttributeValueName(ctx, tenantID, parent.CatalogSourceID, entry.CatalogValueID)
			if err != nil {
				return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
			}
			if catalogValueName == nil {
				return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewNotFoundError(
					fmt.Sprintf("Catalog attribute value '%s' not found.", entry.CatalogValueID)))
			}
		} else {
			catalogValueName, err = h.catalog.GetVariantValueLabel(ctx, tenantID, parent.CatalogSourceID, entry.CatalogValueID)
			if err != nil {
				return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
			}
			if catalogValueName == nil {
				return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewNotFoundError(
					fmt.Sprintf("Catalog variant value '%s' not found.", entry.CatalogValueID)))
			}
		}

		if !externalAttribute.AllowCustom {
			externalValue, err := h.repo.GetExternalValue(ctx, mp, *externalCategoryID, parent.ExternalAttributeID, entry.ExternalValueID)
			if err != nil {
				return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
			}
			if externalValue == nil {
				return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewNotFoundError(
					fmt.Sprintf("External attribute value '%s' not found.", entry.ExternalValueID)))
			}
		}

		existing, err := h.repo.GetValueMapping(ctx, tenantID, parent.ID, entry.CatalogValueID)
		if err != nil {
			return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
		}
		if existing == nil {
			if strings.TrimSpace(entry.ExternalValueID) == "" {
				return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewValidationError("External value id is required."))
			}
			existing = &domain.AttributeValueChannelMapping{
				ID: uuid.New(), AttributeChannelMappingID: parent.ID,
				CatalogValueID: entry.CatalogValueID, ExternalValueID: strings.TrimSpace(entry.ExternalValueID),
			}
			if err := h.repo.AddValueMapping(ctx, tenantID, existing); err != nil {
				return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
			}
		} else {
			if strings.TrimSpace(entry.ExternalValueID) == "" {
				return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewValidationError("External value id is required."))
			}
			existing.ExternalValueID = strings.TrimSpace(entry.ExternalValueID)
			if err := h.repo.UpdateValueMapping(ctx, tenantID, existing); err != nil {
				return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
			}
		}
		upserted = append(upserted, existing)
	}

	dtos := make([]AttributeValueChannelMappingDto, len(upserted))
	for i, mapping := range upserted {
		dto, err := h.enrichValueMapping(ctx, tenantID, mapping, parent)
		if err != nil {
			return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
		}
		dtos[i] = dto
	}
	return sharedkernel.OkOf(dtos)
}

// ListValueMappings, alan eşlemesi altındaki değer eşlemelerini döner.
func (h *Handlers) ListValueMappings(ctx context.Context, tenantID uuid.UUID, code string, catalogCategoryID, mappingID uuid.UUID) sharedkernel.ResultOf[[]AttributeValueChannelMappingDto] {
	marketplace := resolveMarketplace(code)
	if marketplace.IsFailure() {
		return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](marketplace.Err())
	}
	parent, err := h.getOwnedAttributeMapping(ctx, tenantID, marketplace.Value().Code(), catalogCategoryID, mappingID)
	if err != nil {
		return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	if parent == nil {
		return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewNotFoundError("Attribute channel mapping not found."))
	}

	mappings, err := h.repo.ListValueMappings(ctx, tenantID, parent.ID)
	if err != nil {
		return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
	}
	dtos := make([]AttributeValueChannelMappingDto, len(mappings))
	for i, mapping := range mappings {
		dto, err := h.enrichValueMapping(ctx, tenantID, mapping, parent)
		if err != nil {
			return sharedkernel.FailOf[[]AttributeValueChannelMappingDto](sharedkernel.NewInternalError(err.Error()))
		}
		dtos[i] = dto
	}
	return sharedkernel.OkOf(dtos)
}
