package application

import (
	"context"
	"sort"
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// GetProductReadiness, ürünün bağlı pazaryerlerine yayın hazırlığını hesaplar
// (.NET GetProductReadinessHandler portu): kategori eşlemesi + pazaryerinin
// KENDİ şemasındaki zorunlu özellikler + kalem barkodları. PIM'e kopyalanmış
// zorunluluklar burada rol oynamaz.
func (h *Handlers) GetProductReadiness(ctx context.Context, tenantID, productID uuid.UUID) sharedkernel.ResultOf[ProductReadinessDto] {
	sources, err := h.catalog.GetListingSourcesByProduct(ctx, tenantID, productID)
	if err != nil {
		return sharedkernel.FailOf[ProductReadinessDto](sharedkernel.NewInternalError(err.Error()))
	}
	if len(sources) == 0 {
		return sharedkernel.FailOf[ProductReadinessDto](sharedkernel.NewNotFoundError(
			"Ürün bulunamadı veya satılabilir kalemi yok."))
	}
	categoryID := sources[0].CategoryID

	configured, err := h.repo.GetConfiguredMarketplaceCodes(ctx, tenantID)
	if err != nil {
		return sharedkernel.FailOf[ProductReadinessDto](sharedkernel.NewInternalError(err.Error()))
	}
	codes := make([]string, 0, len(configured))
	for code := range configured {
		codes = append(codes, code)
	}
	sort.Strings(codes)

	channels := make([]ChannelReadinessDto, 0, len(codes))
	for _, code := range codes {
		marketplace := sharedkernel.MarketplaceFromPersistence(code)
		channel, err := h.buildChannelReadiness(ctx, tenantID, marketplace, categoryID, sources)
		if err != nil {
			return sharedkernel.FailOf[ProductReadinessDto](sharedkernel.NewInternalError(err.Error()))
		}
		channels = append(channels, channel)
	}
	return sharedkernel.OkOf(ProductReadinessDto{ProductID: productID, Channels: channels})
}

// buildChannelReadiness, tek pazaryeri için hazırlık durumunu hesaplar.
func (h *Handlers) buildChannelReadiness(ctx context.Context, tenantID uuid.UUID, marketplace sharedkernel.Marketplace, categoryID uuid.UUID, sources []CatalogListingSource) (ChannelReadinessDto, error) {
	itemsMissingBarcode := 0
	for _, source := range sources {
		if strings.TrimSpace(source.Barcode) == "" {
			itemsMissingBarcode++
		}
	}

	externalCategoryID, err := h.repo.ResolveExternalCategoryID(ctx, tenantID, marketplace.Code(), categoryID)
	if err != nil {
		return ChannelReadinessDto{}, err
	}
	if externalCategoryID == nil || strings.TrimSpace(*externalCategoryID) == "" {
		return ChannelReadinessDto{
			MarketplaceCode: marketplace.Code(), MarketplaceName: marketplace.Name(),
			CategoryMapped: false, Ready: false,
			TotalItems: len(sources), ItemsMissingBarcode: itemsMissingBarcode,
			MissingAttributes: []MissingChannelAttributeDto{},
		}, nil
	}

	allAttributes, err := h.repo.ListExternalAttributes(ctx, marketplace.Code(), *externalCategoryID)
	if err != nil {
		return ChannelReadinessDto{}, err
	}
	var requiredAttributes []*domain.ExternalCategoryAttribute
	for _, attribute := range allAttributes {
		if attribute.Required {
			requiredAttributes = append(requiredAttributes, attribute)
		}
	}

	// Kategori başına eşleme sayısı pratikte küçüktür; tek sayfada okunur.
	mappings, _, err := h.repo.ListAttributeMappings(ctx, tenantID, marketplace.Code(), categoryID, nil,
		sharedkernel.Pagination{Page: 1, PageSize: sharedkernel.PaginationMaxPageSize})
	if err != nil {
		return ChannelReadinessDto{}, err
	}
	mappingsByExternalID := map[string][]*domain.AttributeChannelMapping{}
	for _, mapping := range mappings {
		mappingsByExternalID[mapping.ExternalAttributeID] = append(mappingsByExternalID[mapping.ExternalAttributeID], mapping)
	}

	missing := []MissingChannelAttributeDto{}
	for _, required := range requiredAttributes {
		mappingList, mapped := mappingsByExternalID[required.ExternalAttributeID]
		if !mapped {
			missing = append(missing, MissingChannelAttributeDto{
				ExternalAttributeID: required.ExternalAttributeID, Name: required.Name,
				Reason: "unmapped", MissingItemCount: len(sources),
			})
			continue
		}
		// Aynı dış özelliğe birden çok PIM kaynağı eşlenebilir (özellik + varyant);
		// kalem, eşlemelerden HERHANGİ biri için değer taşıyorsa özellik dolu sayılır.
		missingItemCount := 0
		for _, source := range sources {
			filled := false
			for _, mapping := range mappingList {
				isVariantSource := mapping.SourceType == domain.SourceCatalogVariant
				for _, selection := range source.Attributes {
					if selection.IsVariant == isVariantSource && selection.SourceID == mapping.CatalogSourceID {
						filled = true
						break
					}
				}
				if filled {
					break
				}
			}
			if !filled {
				missingItemCount++
			}
		}
		if missingItemCount > 0 {
			missing = append(missing, MissingChannelAttributeDto{
				ExternalAttributeID: required.ExternalAttributeID, Name: required.Name,
				Reason: "unfilled", MissingItemCount: missingItemCount,
			})
		}
	}

	return ChannelReadinessDto{
		MarketplaceCode: marketplace.Code(), MarketplaceName: marketplace.Name(),
		CategoryMapped: true, Ready: len(missing) == 0 && itemsMissingBarcode == 0,
		TotalItems: len(sources), ItemsMissingBarcode: itemsMissingBarcode,
		MissingAttributes: missing,
	}, nil
}
