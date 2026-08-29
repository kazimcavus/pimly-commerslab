// Package application, Channels modülünün kullanım senaryolarını, DTO
// sözleşmelerini ve portlarını içerir (.NET Channels.Application karşılığı).
package application

import (
	"context"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// MarketplaceDto, desteklenen pazaryerinin kablo biçimidir.
type MarketplaceDto struct {
	Code         string `json:"code"`
	Name         string `json:"name"`
	IsActive     bool   `json:"is_active"`
	IsConfigured bool   `json:"is_configured"`
}

// MarketplaceConnectionDto, bağlantının kablo biçimidir; gizli alanlar maskelenir
// (api_key_hint son 4 karakterdir).
type MarketplaceConnectionDto struct {
	ID              uuid.UUID `json:"id"`
	MarketplaceCode string    `json:"marketplace_code"`
	SellerID        *string   `json:"seller_id"`
	HasApiKey       bool      `json:"has_api_key"`
	HasApiSecret    bool      `json:"has_api_secret"`
	ApiKeyHint      *string   `json:"api_key_hint"`
	IsEnabled       bool      `json:"is_enabled"`

	// Ayarlar gizli değildir; maskelenmeden döner.
	DisplayName        *string               `json:"display_name"`
	ExternalLocationID *string               `json:"external_location_id"`
	PricesIncludeVat   bool                  `json:"prices_include_vat"`
	ExclusionRules     domain.ExclusionRules `json:"exclusion_rules"`
}

// connectionToDto, bağlantıyı maskeli DTO'ya çevirir.
func connectionToDto(c *domain.MarketplaceConnection) MarketplaceConnectionDto {
	var hint *string
	if c.ApiKey != "" {
		value := c.ApiKey
		if len(value) > 4 {
			value = value[len(value)-4:]
		}
		hint = &value
	}
	return MarketplaceConnectionDto{
		ID: c.ID, MarketplaceCode: c.MarketplaceCode, SellerID: c.SellerID,
		HasApiKey: c.ApiKey != "", HasApiSecret: c.ApiSecret != nil,
		ApiKeyHint: hint, IsEnabled: c.IsEnabled,
		DisplayName:        c.Settings.DisplayName,
		ExternalLocationID: c.Settings.ExternalLocationID,
		PricesIncludeVat:   c.Settings.PricesIncludeVat,
		ExclusionRules:     c.Settings.ExclusionRules,
	}
}

// TaxonomySyncRunDto, taksonomi senkron işinin kablo biçimidir.
type TaxonomySyncRunDto struct {
	ID              uuid.UUID  `json:"id"`
	MarketplaceCode string     `json:"marketplace_code"`
	Status          string     `json:"status"`
	CreatedAt       time.Time  `json:"created_at"`
	StartedAt       *time.Time `json:"started_at"`
	CompletedAt     *time.Time `json:"completed_at"`
	ProcessedCount  int        `json:"processed_count"`
	TotalEstimate   *int       `json:"total_estimate"`
	ErrorMessage    *string    `json:"error_message"`
}

// taxonomyRunToDto, iş kaydını DTO'ya çevirir.
func taxonomyRunToDto(r *domain.TaxonomySyncRun) TaxonomySyncRunDto {
	return TaxonomySyncRunDto{
		ID: r.ID, MarketplaceCode: r.MarketplaceCode, Status: string(r.Status),
		CreatedAt: r.CreatedAt, StartedAt: r.StartedAt, CompletedAt: r.CompletedAt,
		ProcessedCount: r.ProcessedCount, TotalEstimate: r.TotalEstimate, ErrorMessage: r.ErrorMessage,
	}
}

// TaxonomyStatusDto, taksonomi senkronunun özet durumudur.
type TaxonomyStatusDto struct {
	MarketplaceCode     string              `json:"marketplace_code"`
	IsSyncActive        bool                `json:"is_sync_active"`
	ActiveSyncRunID     *uuid.UUID          `json:"active_sync_run_id"`
	LastCompletedAt     *time.Time          `json:"last_completed_at"`
	CachedCategoryCount int                 `json:"cached_category_count"`
	LastCompletedRun    *TaxonomySyncRunDto `json:"last_completed_run"`
}

// ExternalCategoryDto, harici kategori arama sonucudur.
type ExternalCategoryDto struct {
	ID               uuid.UUID `json:"id"`
	ExternalID       string    `json:"external_id"`
	Name             string    `json:"name"`
	ParentExternalID *string   `json:"parent_external_id"`
	Path             string    `json:"path"`
	IsLeaf           bool      `json:"is_leaf"`
	SyncedAt         time.Time `json:"synced_at"`
}

// externalCategoryToDto, cache kaydını DTO'ya çevirir.
func externalCategoryToDto(c *domain.ExternalCategory) ExternalCategoryDto {
	return ExternalCategoryDto{
		ID: c.ID, ExternalID: c.ExternalID, Name: c.Name, ParentExternalID: c.ParentExternalID,
		Path: c.Path, IsLeaf: c.IsLeaf, SyncedAt: c.SyncedAt,
	}
}

// ExternalAttributeValueDto, harici özellik değerinin kablo biçimidir.
type ExternalAttributeValueDto struct {
	ExternalAttributeID string    `json:"external_attribute_id"`
	ExternalValueID     string    `json:"external_value_id"`
	Name                string    `json:"name"`
	SyncedAt            time.Time `json:"synced_at"`
}

// ExternalCategoryAttributeDto, harici kategori özelliğinin kablo biçimidir.
type ExternalCategoryAttributeDto struct {
	ExternalCategoryID  string                      `json:"external_category_id"`
	ExternalAttributeID string                      `json:"external_attribute_id"`
	Name                string                      `json:"name"`
	Required            bool                        `json:"required"`
	AllowCustom         bool                        `json:"allow_custom"`
	IsVariant           bool                        `json:"is_variant"`
	SyncedAt            time.Time                   `json:"synced_at"`
	Values              []ExternalAttributeValueDto `json:"values"`
	IsSlicer            bool                        `json:"is_slicer"`
}

// ProductImportRunSummaryDto, import işinin özet kablo biçimidir.
type ProductImportRunSummaryDto struct {
	ID                uuid.UUID  `json:"id"`
	MarketplaceCode   string     `json:"marketplace_code"`
	Status            string     `json:"status"`
	CreatedAt         time.Time  `json:"created_at"`
	StartedAt         *time.Time `json:"started_at"`
	CompletedAt       *time.Time `json:"completed_at"`
	TotalProducts     *int       `json:"total_products"`
	ProcessedProducts int        `json:"processed_products"`
	ImportedProducts  int        `json:"imported_products"`
	SkippedProducts   int        `json:"skipped_products"`
	FailedProducts    int        `json:"failed_products"`
}

// ProductImportErrorDto, import hata kaydının kablo biçimidir.
type ProductImportErrorDto struct {
	ProductMainID string  `json:"product_main_id"`
	Barcode       *string `json:"barcode"`
	Message       string  `json:"message"`
}

// ProductImportRunDto, import işinin ayrıntı kablo biçimidir.
type ProductImportRunDto struct {
	ProductImportRunSummaryDto
	ErrorMessage *string                 `json:"error_message"`
	Errors       []ProductImportErrorDto `json:"errors"`
}

// importRunToSummaryDto ve importRunToDto, iş kaydını DTO'lara çevirir.
func importRunToSummaryDto(r *domain.ProductImportRun) ProductImportRunSummaryDto {
	return ProductImportRunSummaryDto{
		ID: r.ID, MarketplaceCode: r.MarketplaceCode, Status: string(r.Status),
		CreatedAt: r.CreatedAt, StartedAt: r.StartedAt, CompletedAt: r.CompletedAt,
		TotalProducts: r.TotalProducts, ProcessedProducts: r.ProcessedProducts,
		ImportedProducts: r.ImportedProducts, SkippedProducts: r.SkippedProducts,
		FailedProducts: r.FailedProducts,
	}
}

func importRunToDto(r *domain.ProductImportRun) ProductImportRunDto {
	errors := make([]ProductImportErrorDto, len(r.Errors))
	for i, e := range r.Errors {
		errors[i] = ProductImportErrorDto{ProductMainID: e.ProductMainID, Barcode: e.Barcode, Message: e.Message}
	}
	return ProductImportRunDto{
		ProductImportRunSummaryDto: importRunToSummaryDto(r),
		ErrorMessage:               r.ErrorMessage,
		Errors:                     errors,
	}
}

// ProductPublicationRunSummaryDto, yayın işinin özet kablo biçimidir.
type ProductPublicationRunSummaryDto struct {
	ID              uuid.UUID  `json:"id"`
	MarketplaceCode string     `json:"marketplace_code"`
	Status          string     `json:"status"`
	CreatedAt       time.Time  `json:"created_at"`
	StartedAt       *time.Time `json:"started_at"`
	CompletedAt     *time.Time `json:"completed_at"`
	TotalItems      *int       `json:"total_items"`
	ProcessedItems  int        `json:"processed_items"`
	PublishedItems  int        `json:"published_items"`
	FailedItems     int        `json:"failed_items"`
}

// ProductPublicationErrorDto, yayın hata kaydının kablo biçimidir.
type ProductPublicationErrorDto struct {
	ProductItemID uuid.UUID `json:"product_item_id"`
	Message       string    `json:"message"`
}

// ProductPublicationRunDto, yayın işinin ayrıntı kablo biçimidir.
type ProductPublicationRunDto struct {
	ProductPublicationRunSummaryDto
	ErrorMessage *string                      `json:"error_message"`
	Errors       []ProductPublicationErrorDto `json:"errors"`
}

// publicationRunToDto, iş kaydını ayrıntı DTO'suna çevirir.
func publicationRunToDto(r *domain.ProductPublicationRun) ProductPublicationRunDto {
	errors := make([]ProductPublicationErrorDto, len(r.Errors))
	for i, e := range r.Errors {
		errors[i] = ProductPublicationErrorDto{ProductItemID: e.ProductItemID, Message: e.Message}
	}
	return ProductPublicationRunDto{
		ProductPublicationRunSummaryDto: ProductPublicationRunSummaryDto{
			ID: r.ID, MarketplaceCode: r.MarketplaceCode, Status: string(r.Status),
			CreatedAt: r.CreatedAt, StartedAt: r.StartedAt, CompletedAt: r.CompletedAt,
			TotalItems: r.TotalItems, ProcessedItems: r.ProcessedItems,
			PublishedItems: r.PublishedItems, FailedItems: r.FailedItems,
		},
		ErrorMessage: r.ErrorMessage,
		Errors:       errors,
	}
}

// --- Eşleme DTO'ları ---

// CatalogCategorySnapshotDto, Catalog kategori özetidir.
type CatalogCategorySnapshotDto struct {
	ID   uuid.UUID `json:"id"`
	Name string    `json:"name"`
	Code *string   `json:"code"`
}

// ExternalCategorySummaryDto, harici kategori özetidir.
type ExternalCategorySummaryDto struct {
	ExternalID string    `json:"external_id"`
	Name       string    `json:"name"`
	Path       string    `json:"path"`
	IsLeaf     bool      `json:"is_leaf"`
	SyncedAt   time.Time `json:"synced_at"`
}

// CategoryChannelMappingDto, kategori eşlemesinin kablo biçimidir.
type CategoryChannelMappingDto struct {
	ID                uuid.UUID                   `json:"id"`
	CatalogCategoryID uuid.UUID                   `json:"catalog_category_id"`
	MarketplaceCode   string                      `json:"marketplace_code"`
	ExternalID        string                      `json:"external_id"`
	CatalogCategory   *CatalogCategorySnapshotDto `json:"catalog_category"`
	ExternalCategory  *ExternalCategorySummaryDto `json:"external_category"`
}

// CatalogAttributeSnapshotDto ve CatalogVariantSnapshotDto, Catalog kaynak özetleridir.
type CatalogAttributeSnapshotDto struct {
	ID   uuid.UUID `json:"id"`
	Key  string    `json:"key"`
	Name string    `json:"name"`
}

// CatalogVariantSnapshotDto, varyant ekseni özetidir.
type CatalogVariantSnapshotDto struct {
	ID   uuid.UUID `json:"id"`
	Key  string    `json:"key"`
	Name string    `json:"name"`
}

// ExternalCategoryAttributeSummaryDto, harici özelliğin özetidir.
type ExternalCategoryAttributeSummaryDto struct {
	ExternalAttributeID string `json:"external_attribute_id"`
	Name                string `json:"name"`
	Required            bool   `json:"required"`
	AllowCustom         bool   `json:"allow_custom"`
	IsVariant           bool   `json:"is_variant"`
	IsSlicer            bool   `json:"is_slicer"`
}

// AttributeChannelMappingDto, alan eşlemesinin kablo biçimidir.
type AttributeChannelMappingDto struct {
	ID                  uuid.UUID                            `json:"id"`
	CatalogCategoryID   uuid.UUID                            `json:"catalog_category_id"`
	MarketplaceCode     string                               `json:"marketplace_code"`
	SourceType          string                               `json:"source_type"`
	CatalogSourceID     uuid.UUID                            `json:"catalog_source_id"`
	ExternalAttributeID string                               `json:"external_attribute_id"`
	CatalogAttribute    *CatalogAttributeSnapshotDto         `json:"catalog_attribute"`
	CatalogVariant      *CatalogVariantSnapshotDto           `json:"catalog_variant"`
	ExternalAttribute   *ExternalCategoryAttributeSummaryDto `json:"external_attribute"`
}

// ExternalAttributeValueSummaryDto, harici değerin özetidir.
type ExternalAttributeValueSummaryDto struct {
	ExternalValueID string `json:"external_value_id"`
	Name            string `json:"name"`
}

// AttributeValueChannelMappingDto, değer eşlemesinin kablo biçimidir.
type AttributeValueChannelMappingDto struct {
	ID                        uuid.UUID                         `json:"id"`
	AttributeChannelMappingID uuid.UUID                         `json:"attribute_channel_mapping_id"`
	CatalogValueID            uuid.UUID                         `json:"catalog_value_id"`
	ExternalValueID           string                            `json:"external_value_id"`
	CatalogValueName          *string                           `json:"catalog_value_name"`
	ExternalValue             *ExternalAttributeValueSummaryDto `json:"external_value"`
}

// --- Hazırlık (readiness) DTO'ları ---

// MissingChannelAttributeDto, pazaryerinin zorunlu tuttuğu ama üründe eksik
// özelliktir; reason "unmapped" | "unfilled".
type MissingChannelAttributeDto struct {
	ExternalAttributeID string `json:"external_attribute_id"`
	Name                string `json:"name"`
	Reason              string `json:"reason"`
	MissingItemCount    int    `json:"missing_item_count"`
}

// ChannelReadinessDto, tek pazaryeri için hazırlık durumudur.
type ChannelReadinessDto struct {
	MarketplaceCode     string                       `json:"marketplace_code"`
	MarketplaceName     string                       `json:"marketplace_name"`
	CategoryMapped      bool                         `json:"category_mapped"`
	Ready               bool                         `json:"ready"`
	TotalItems          int                          `json:"total_items"`
	ItemsMissingBarcode int                          `json:"items_missing_barcode"`
	MissingAttributes   []MissingChannelAttributeDto `json:"missing_attributes"`
}

// ProductReadinessDto, ürünün bağlı pazaryerlerine yayın hazırlık özetidir.
type ProductReadinessDto struct {
	ProductID uuid.UUID             `json:"product_id"`
	Channels  []ChannelReadinessDto `json:"channels"`
}

// --- Catalog gateway kayıtları ---

// CatalogListingSelection, kalem üzerindeki tek özellik/varyant seçimidir.
type CatalogListingSelection struct {
	IsVariant  bool
	SourceID   uuid.UUID
	ValueID    uuid.UUID
	ValueLabel string
}

// CatalogListingSource, kalemin pazaryerine giden içerik kaynağıdır.
type CatalogListingSource struct {
	ProductItemID     uuid.UUID
	ProductID         uuid.UUID
	CategoryID        uuid.UUID
	Title             string
	Description       *string
	BrandName         *string
	BrandExternalCode *string
	ModelCode         string
	Barcode           string
	Sku               *string
	Attributes        []CatalogListingSelection
	ImageURLs         []string
}

// MarketplaceCategoryAttributeNode, pazaryerinden çekilen özellik düğümüdür.
type MarketplaceCategoryAttributeNode struct {
	ExternalAttributeID string
	Name                string
	Required            bool
	AllowCustom         bool
	IsVariant           bool
	IsSlicer            bool
	Values              []MarketplaceAttributeValueNode
}

// MarketplaceAttributeValueNode, pazaryerinden çekilen değer düğümüdür.
type MarketplaceAttributeValueNode struct {
	ExternalValueID string
	Name            string
}

// MarketplaceCredentials, Trendyol API kimlik bilgileridir.
type MarketplaceCredentials struct {
	SellerID  *string
	ApiKey    string
	ApiSecret *string
}

// CategoryAttributesClient, pazaryeri kategori özelliklerini çeken porttur.
type CategoryAttributesClient interface {
	FetchCategoryAttributes(ctx context.Context, credentials *MarketplaceCredentials, externalCategoryID string) sharedkernel.ResultOf[[]MarketplaceCategoryAttributeNode]
}
