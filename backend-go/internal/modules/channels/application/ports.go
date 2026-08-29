package application

import (
	"context"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// ChannelsRepository, Channels şemasının kalıcılık portudur. Tüm metodlar
// tenant kimliğini açıkça alır; taksonomi tabloları ve harici katalog cache'i
// pazaryeri-global olduğundan tenant parametresi taşımaz.
type ChannelsRepository interface {
	// --- Bağlantılar ---
	GetConnection(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) (*domain.MarketplaceConnection, error)
	GetConfiguredMarketplaceCodes(ctx context.Context, tenantID uuid.UUID) (map[string]struct{}, error)
	AddConnection(ctx context.Context, tenantID uuid.UUID, connection *domain.MarketplaceConnection) error
	UpdateConnection(ctx context.Context, tenantID uuid.UUID, connection *domain.MarketplaceConnection) error

	// --- Taksonomi senkron işleri (pazaryeri-global) ---
	GetActiveTaxonomyRun(ctx context.Context, marketplaceCode string) (*domain.TaxonomySyncRun, error)
	GetLatestCompletedTaxonomyRun(ctx context.Context, marketplaceCode string) (*domain.TaxonomySyncRun, error)
	GetTaxonomyRun(ctx context.Context, id uuid.UUID) (*domain.TaxonomySyncRun, error)
	AddTaxonomyRun(ctx context.Context, run *domain.TaxonomySyncRun) error

	// --- Harici katalog cache'i (pazaryeri-global) ---
	CountExternalCategories(ctx context.Context, marketplaceCode string) (int, error)
	SearchExternalCategories(ctx context.Context, marketplaceCode string, query *string, limit int) ([]*domain.ExternalCategory, error)
	GetExternalCategory(ctx context.Context, marketplaceCode, externalID string) (*domain.ExternalCategory, error)
	ListExternalAttributes(ctx context.Context, marketplaceCode, externalCategoryID string) ([]*domain.ExternalCategoryAttribute, error)
	GetExternalAttribute(ctx context.Context, marketplaceCode, externalCategoryID, externalAttributeID string) (*domain.ExternalCategoryAttribute, error)
	ListExternalValues(ctx context.Context, marketplaceCode, externalCategoryID string) ([]*domain.ExternalAttributeValue, error)
	GetExternalValue(ctx context.Context, marketplaceCode, externalCategoryID, externalAttributeID, externalValueID string) (*domain.ExternalAttributeValue, error)

	// RefreshExternalAttributes, kategorinin özellik+değer cache'ini pazaryeri
	// verisiyle tek transaction'da değiştirir.
	RefreshExternalAttributes(ctx context.Context, marketplaceCode, externalCategoryID string, nodes []MarketplaceCategoryAttributeNode, syncedAt time.Time) error

	// --- İmport işleri ---
	GetActiveImportRun(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) (*domain.ProductImportRun, error)
	GetImportRun(ctx context.Context, tenantID uuid.UUID, id uuid.UUID) (*domain.ProductImportRun, error)
	ListRecentImportRuns(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, limit int) ([]*domain.ProductImportRun, error)
	AddImportRun(ctx context.Context, run *domain.ProductImportRun) error

	// --- Yayın işleri ---
	GetActivePublicationRun(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) (*domain.ProductPublicationRun, error)
	GetPublicationRun(ctx context.Context, tenantID uuid.UUID, id uuid.UUID) (*domain.ProductPublicationRun, error)
	AddPublicationRun(ctx context.Context, run *domain.ProductPublicationRun) error

	// --- Kategori eşlemeleri ---
	GetCategoryMapping(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID) (*domain.CategoryChannelMapping, error)
	ResolveExternalCategoryID(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID) (*string, error)
	ListCategoryMappings(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID *uuid.UUID, p sharedkernel.Pagination) ([]*domain.CategoryChannelMapping, int, error)
	AddCategoryMapping(ctx context.Context, tenantID uuid.UUID, mapping *domain.CategoryChannelMapping) error
	UpdateCategoryMapping(ctx context.Context, tenantID uuid.UUID, mapping *domain.CategoryChannelMapping) error
	RemoveCategoryMapping(ctx context.Context, tenantID uuid.UUID, id uuid.UUID) error

	// --- Alan eşlemeleri ---
	GetAttributeMappingByID(ctx context.Context, tenantID uuid.UUID, id uuid.UUID) (*domain.AttributeChannelMapping, error)
	GetAttributeMapping(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID, sourceType domain.AttributeMappingSourceType, catalogSourceID uuid.UUID) (*domain.AttributeChannelMapping, error)
	ListAttributeMappings(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID, sourceType *domain.AttributeMappingSourceType, p sharedkernel.Pagination) ([]*domain.AttributeChannelMapping, int, error)
	AddAttributeMapping(ctx context.Context, tenantID uuid.UUID, mapping *domain.AttributeChannelMapping) error
	UpdateAttributeMapping(ctx context.Context, tenantID uuid.UUID, mapping *domain.AttributeChannelMapping) error
	RemoveAttributeMapping(ctx context.Context, tenantID uuid.UUID, id uuid.UUID) error

	// --- Değer eşlemeleri ---
	GetValueMapping(ctx context.Context, tenantID uuid.UUID, attributeMappingID, catalogValueID uuid.UUID) (*domain.AttributeValueChannelMapping, error)
	ListValueMappings(ctx context.Context, tenantID uuid.UUID, attributeMappingID uuid.UUID) ([]*domain.AttributeValueChannelMapping, error)
	AddValueMapping(ctx context.Context, tenantID uuid.UUID, mapping *domain.AttributeValueChannelMapping) error
	UpdateValueMapping(ctx context.Context, tenantID uuid.UUID, mapping *domain.AttributeValueChannelMapping) error
}

// CatalogGateway, Channels'ın Catalog şemasından okuduğu ACL portudur
// (.NET Pimly.Integration gateway'lerinin süreç içi karşılığı).
type CatalogGateway interface {
	// GetCategorySnapshot, kategori özetini döner; yoksa nil.
	GetCategorySnapshot(ctx context.Context, tenantID, categoryID uuid.UUID) (*CatalogCategorySnapshotDto, error)

	// GetAttributeSnapshot, özellik özetini döner; yoksa nil.
	GetAttributeSnapshot(ctx context.Context, tenantID, attributeID uuid.UUID) (*CatalogAttributeSnapshotDto, error)

	// AttributeBelongsToCategory, özelliğin kategoriye atanmış olup olmadığını döner.
	AttributeBelongsToCategory(ctx context.Context, tenantID, categoryID, attributeID uuid.UUID) (bool, error)

	// GetAttributeValueName, özellik değerinin adını döner; yoksa nil.
	GetAttributeValueName(ctx context.Context, tenantID, attributeID, valueID uuid.UUID) (*string, error)

	// GetVariantSnapshot, varyant ekseni özetini döner; yoksa nil.
	GetVariantSnapshot(ctx context.Context, tenantID, variantID uuid.UUID) (*CatalogVariantSnapshotDto, error)

	// GetVariantValueLabel, varyant değerinin etiketini döner; yoksa nil.
	GetVariantValueLabel(ctx context.Context, tenantID, variantID, valueID uuid.UUID) (*string, error)

	// GetListingSourcesByProduct, ürünün kalem başına içerik kaynaklarını döner;
	// ürün yoksa veya kalemi yoksa boş liste.
	GetListingSourcesByProduct(ctx context.Context, tenantID, productID uuid.UUID) ([]CatalogListingSource, error)
}
