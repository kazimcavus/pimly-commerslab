// Package domain, Channels modülünün varlıklarını içerir (.NET Channels.Domain
// karşılığı): pazaryeri bağlantıları, harici katalog cache'i (kategori/özellik/
// değer), eşlemeler (kategori/özellik/değer) ve iş kayıtları (taksonomi senkronu,
// ürün import'u, ürün yayını). Kuyruk tabloları tenant'lar arası ortaktır;
// worker'lar FOR UPDATE SKIP LOCKED ile beslenir.
package domain

import (
	"strings"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// trimToNil, opsiyonel dizgiyi kırpar; boş değer nil'e çevrilir.
func trimToNil(s *string) *string {
	if s == nil {
		return nil
	}
	trimmed := strings.TrimSpace(*s)
	if trimmed == "" {
		return nil
	}
	return &trimmed
}

// MarketplaceConnection, tenant'ın bir pazaryerine ait API kimlik bilgileridir.
type MarketplaceConnection struct {
	ID              uuid.UUID
	MarketplaceCode string
	SellerID        *string
	ApiKey          string
	ApiSecret       *string
	IsEnabled       bool
}

// NewMarketplaceConnection, doğrulanmış yeni bağlantı oluşturur.
func NewMarketplaceConnection(marketplaceCode string, sellerID *string, apiKey string, apiSecret *string, isEnabled bool) sharedkernel.ResultOf[*MarketplaceConnection] {
	if strings.TrimSpace(apiKey) == "" {
		return sharedkernel.FailOf[*MarketplaceConnection](sharedkernel.NewValidationError("Api key is required."))
	}
	return sharedkernel.OkOf(&MarketplaceConnection{
		ID: uuid.New(), MarketplaceCode: marketplaceCode,
		SellerID: trimToNil(sellerID), ApiKey: strings.TrimSpace(apiKey),
		ApiSecret: trimToNil(apiSecret), IsEnabled: isEnabled,
	})
}

// Update, bağlantı kimlik bilgilerini günceller.
func (c *MarketplaceConnection) Update(sellerID *string, apiKey string, apiSecret *string, isEnabled bool) sharedkernel.Result {
	if strings.TrimSpace(apiKey) == "" {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Api key is required."))
	}
	c.SellerID = trimToNil(sellerID)
	c.ApiKey = strings.TrimSpace(apiKey)
	c.ApiSecret = trimToNil(apiSecret)
	c.IsEnabled = isEnabled
	return sharedkernel.Ok()
}

// ExternalCategory, pazaryerinden cache'lenen harici kategori kaydıdır.
type ExternalCategory struct {
	ID               uuid.UUID
	MarketplaceCode  string
	ExternalID       string
	Name             string
	ParentExternalID *string
	Path             string
	IsLeaf           bool
	SyncedAt         time.Time
}

// ExternalCategoryAttribute, pazaryeri kategorisine ait cache'lenmiş özellik kaydıdır.
type ExternalCategoryAttribute struct {
	ID                  uuid.UUID
	MarketplaceCode     string
	ExternalCategoryID  string
	ExternalAttributeID string
	Name                string
	Required            bool
	AllowCustom         bool
	IsVariant           bool
	IsSlicer            bool
	SyncedAt            time.Time
}

// ExternalAttributeValue, harici özelliğe ait cache'lenmiş değer kaydıdır.
type ExternalAttributeValue struct {
	ID                  uuid.UUID
	MarketplaceCode     string
	ExternalCategoryID  string
	ExternalAttributeID string
	ExternalValueID     string
	Name                string
	SyncedAt            time.Time
}

// CategoryChannelMapping, Catalog kategorisi ↔ pazaryeri kategorisi eşlemesidir.
type CategoryChannelMapping struct {
	ID                uuid.UUID
	CatalogCategoryID uuid.UUID
	MarketplaceCode   string
	ExternalID        string
}

// AttributeMappingSourceType, alan eşlemesinin catalog kaynak türüdür;
// kabloda ve veritabanında "catalog_attribute" / "catalog_variant" taşınır.
type AttributeMappingSourceType string

// Kaynak türleri.
const (
	SourceCatalogAttribute AttributeMappingSourceType = "catalog_attribute"
	SourceCatalogVariant   AttributeMappingSourceType = "catalog_variant"
)

// ParseSourceType, kullanıcı girdisini kaynak türüne çözer
// (.NET AttributeMappingSourceTypeParser.Parse portu).
func ParseSourceType(value string) sharedkernel.ResultOf[AttributeMappingSourceType] {
	if strings.TrimSpace(value) == "" {
		return sharedkernel.FailOf[AttributeMappingSourceType](sharedkernel.NewValidationError("Source type is required."))
	}
	switch strings.ToLower(strings.TrimSpace(value)) {
	case string(SourceCatalogAttribute):
		return sharedkernel.OkOf(SourceCatalogAttribute)
	case string(SourceCatalogVariant):
		return sharedkernel.OkOf(SourceCatalogVariant)
	default:
		return sharedkernel.FailOf[AttributeMappingSourceType](sharedkernel.NewValidationError(
			"Source type must be catalog_attribute or catalog_variant."))
	}
}

// AttributeChannelMapping, Catalog özelliği/varyantı ↔ harici özellik eşlemesidir.
type AttributeChannelMapping struct {
	ID                  uuid.UUID
	MarketplaceCode     string
	CatalogCategoryID   uuid.UUID
	SourceType          AttributeMappingSourceType
	CatalogSourceID     uuid.UUID
	ExternalAttributeID string
}

// AttributeValueChannelMapping, alan eşlemesi altında catalog değeri ↔ harici
// değer eşlemesidir.
type AttributeValueChannelMapping struct {
	ID                        uuid.UUID
	AttributeChannelMappingID uuid.UUID
	CatalogValueID            uuid.UUID
	ExternalValueID           string
}

// --- Taksonomi senkron iş kaydı ---

// TaxonomySyncStatus, taksonomi senkron işinin durumudur (kabloda küçük harf).
type TaxonomySyncStatus string

// Taksonomi senkron durumları.
const (
	TaxonomyPending   TaxonomySyncStatus = "pending"
	TaxonomyRunning   TaxonomySyncStatus = "running"
	TaxonomyCompleted TaxonomySyncStatus = "completed"
	TaxonomyFailed    TaxonomySyncStatus = "failed"
	TaxonomyCancelled TaxonomySyncStatus = "cancelled"
)

// TaxonomySyncRun, pazaryeri kategori ağacı senkron iş kaydıdır
// (pazaryeri-global: tenant kolonu yoktur).
type TaxonomySyncRun struct {
	ID              uuid.UUID
	MarketplaceCode string
	Status          TaxonomySyncStatus
	CreatedAt       time.Time
	StartedAt       *time.Time
	CompletedAt     *time.Time
	ProcessedCount  int
	TotalEstimate   *int
	ErrorMessage    *string
}

// NewTaxonomySyncRun, pending durumda yeni iş kaydı oluşturur.
func NewTaxonomySyncRun(marketplaceCode string, createdAt time.Time) *TaxonomySyncRun {
	return &TaxonomySyncRun{
		ID: uuid.New(), MarketplaceCode: marketplaceCode,
		Status: TaxonomyPending, CreatedAt: createdAt,
	}
}

// MarkRunning, işi running durumuna alır; yalnızca pending işler başlatılabilir.
func (r *TaxonomySyncRun) MarkRunning(startedAt time.Time) sharedkernel.Result {
	if r.Status != TaxonomyPending {
		return sharedkernel.Fail(sharedkernel.NewConflictError("Only pending taxonomy sync runs can be started."))
	}
	r.Status = TaxonomyRunning
	r.StartedAt = &startedAt
	return sharedkernel.Ok()
}

// MarkCompleted, işi başarıyla tamamlar.
func (r *TaxonomySyncRun) MarkCompleted(completedAt time.Time, processedCount int) sharedkernel.Result {
	if r.Status != TaxonomyRunning {
		return sharedkernel.Fail(sharedkernel.NewConflictError("Only running taxonomy sync runs can be completed."))
	}
	r.Status = TaxonomyCompleted
	r.CompletedAt = &completedAt
	r.ProcessedCount = processedCount
	r.TotalEstimate = &processedCount
	r.ErrorMessage = nil
	return sharedkernel.Ok()
}

// MarkFailed, işi hata ile sonlandırır.
func (r *TaxonomySyncRun) MarkFailed(completedAt time.Time, errorMessage string) sharedkernel.Result {
	if r.Status != TaxonomyRunning {
		return sharedkernel.Fail(sharedkernel.NewConflictError("Only running taxonomy sync runs can be marked as failed."))
	}
	r.Status = TaxonomyFailed
	r.CompletedAt = &completedAt
	message := strings.TrimSpace(errorMessage)
	if message == "" {
		message = "Taxonomy sync failed."
	}
	r.ErrorMessage = &message
	return sharedkernel.Ok()
}

// --- Ürün import iş kaydı ---

// ProductImportStatus, import işinin durumudur (kabloda snake_case).
type ProductImportStatus string

// Import durumları.
const (
	ImportPending             ProductImportStatus = "pending"
	ImportRunning             ProductImportStatus = "running"
	ImportCompleted           ProductImportStatus = "completed"
	ImportCompletedWithErrors ProductImportStatus = "completed_with_errors"
	ImportFailed              ProductImportStatus = "failed"
	ImportCancelled           ProductImportStatus = "cancelled"
)

// ImportMaxErrors, bir import işinde saklanan en fazla hata kaydı sayısıdır.
const ImportMaxErrors = 500

// ImportErrorMessageMaxLength, hata mesajının azami uzunluğudur.
const ImportErrorMessageMaxLength = 1000

// ProductImportError, tek bir ürün grubu için oluşan import hata kaydıdır.
type ProductImportError struct {
	ID            uuid.UUID
	ProductMainID string
	Barcode       *string
	Message       string
}

// ProductImportRun, pazaryerinden ürün import iş kaydıdır; worker kuyruğu bu
// tablo üzerinden FOR UPDATE SKIP LOCKED ile beslenir.
type ProductImportRun struct {
	ID                uuid.UUID
	TenantID          uuid.UUID
	MarketplaceCode   string
	Status            ProductImportStatus
	CreatedAt         time.Time
	StartedAt         *time.Time
	CompletedAt       *time.Time
	TotalProducts     *int
	ProcessedProducts int
	ImportedProducts  int
	SkippedProducts   int
	FailedProducts    int
	ErrorMessage      *string
	Errors            []ProductImportError
}

// NewProductImportRun, pending durumda yeni import işi oluşturur.
func NewProductImportRun(tenantID uuid.UUID, marketplaceCode string, createdAt time.Time) sharedkernel.ResultOf[*ProductImportRun] {
	if tenantID == uuid.Nil {
		return sharedkernel.FailOf[*ProductImportRun](sharedkernel.NewValidationError("Tenant id is required."))
	}
	return sharedkernel.OkOf(&ProductImportRun{
		ID: uuid.New(), TenantID: tenantID, MarketplaceCode: marketplaceCode,
		Status: ImportPending, CreatedAt: createdAt,
	})
}

// MarkRunning, işi running durumuna alır.
func (r *ProductImportRun) MarkRunning(startedAt time.Time) sharedkernel.Result {
	if r.Status != ImportPending {
		return sharedkernel.Fail(sharedkernel.NewConflictError("Only pending product import runs can be started."))
	}
	r.Status = ImportRunning
	r.StartedAt = &startedAt
	return sharedkernel.Ok()
}

// UpdateProgress, ilerleme sayaçlarını günceller.
func (r *ProductImportRun) UpdateProgress(processed, imported, skipped, failed int, total *int) {
	r.ProcessedProducts = processed
	r.ImportedProducts = imported
	r.SkippedProducts = skipped
	r.FailedProducts = failed
	r.TotalProducts = total
}

// AddError, ürün bazlı hata kaydı ekler; sınır aşıldıysa eklemez ve false döner.
func (r *ProductImportRun) AddError(productMainID string, barcode *string, message string) bool {
	if len(r.Errors) >= ImportMaxErrors {
		return false
	}
	normalized := strings.TrimSpace(message)
	if normalized == "" {
		normalized = "Import failed."
	}
	if len([]rune(normalized)) > ImportErrorMessageMaxLength {
		normalized = string([]rune(normalized)[:ImportErrorMessageMaxLength])
	}
	mainID := strings.TrimSpace(productMainID)
	if mainID == "" {
		mainID = "-"
	}
	r.Errors = append(r.Errors, ProductImportError{
		ID: uuid.New(), ProductMainID: mainID, Barcode: trimToNil(barcode), Message: normalized})
	return true
}

// MarkCompleted, işi tamamlar; hata alan grup varsa completed_with_errors olur.
func (r *ProductImportRun) MarkCompleted(completedAt time.Time) sharedkernel.Result {
	if r.Status != ImportRunning {
		return sharedkernel.Fail(sharedkernel.NewConflictError("Only running product import runs can be completed."))
	}
	if r.FailedProducts > 0 {
		r.Status = ImportCompletedWithErrors
	} else {
		r.Status = ImportCompleted
	}
	r.CompletedAt = &completedAt
	r.ErrorMessage = nil
	return sharedkernel.Ok()
}

// MarkFailed, işi altyapı hatasıyla sonlandırır.
func (r *ProductImportRun) MarkFailed(completedAt time.Time, errorMessage string) sharedkernel.Result {
	if r.Status != ImportRunning {
		return sharedkernel.Fail(sharedkernel.NewConflictError("Only running product import runs can be marked as failed."))
	}
	r.Status = ImportFailed
	r.CompletedAt = &completedAt
	message := strings.TrimSpace(errorMessage)
	if message == "" {
		message = "Product import failed."
	}
	r.ErrorMessage = &message
	return sharedkernel.Ok()
}

// --- Ürün yayın iş kaydı ---

// PublicationStatus, yayın işinin durumudur (kabloda snake_case).
type PublicationStatus string

// Yayın durumları.
const (
	PublicationPending             PublicationStatus = "pending"
	PublicationRunning             PublicationStatus = "running"
	PublicationCompleted           PublicationStatus = "completed"
	PublicationCompletedWithErrors PublicationStatus = "completed_with_errors"
	PublicationFailed              PublicationStatus = "failed"
)

// ProductPublicationError, tek kalem için yayın hata kaydıdır.
type ProductPublicationError struct {
	ID            uuid.UUID
	ProductItemID uuid.UUID
	Message       string
}

// ProductPublicationRun, ürün yayın (publish) iş kaydıdır.
type ProductPublicationRun struct {
	ID              uuid.UUID
	TenantID        uuid.UUID
	MarketplaceCode string
	Status          PublicationStatus
	CreatedAt       time.Time
	StartedAt       *time.Time
	CompletedAt     *time.Time
	TotalItems      *int
	ProcessedItems  int
	PublishedItems  int
	FailedItems     int
	ErrorMessage    *string
	Errors          []ProductPublicationError
}

// PublicationMaxErrors, bir yayın işinde saklanan en fazla hata kaydı sayısıdır.
const PublicationMaxErrors = 500

// PublicationErrorMessageMaxLength, hata mesajının azami uzunluğudur.
const PublicationErrorMessageMaxLength = 1000

// NewProductPublicationRun, pending durumda yeni yayın işi oluşturur.
func NewProductPublicationRun(tenantID uuid.UUID, marketplaceCode string, createdAt time.Time) sharedkernel.ResultOf[*ProductPublicationRun] {
	if tenantID == uuid.Nil {
		return sharedkernel.FailOf[*ProductPublicationRun](sharedkernel.NewValidationError("Tenant id is required."))
	}
	return sharedkernel.OkOf(&ProductPublicationRun{
		ID: uuid.New(), TenantID: tenantID, MarketplaceCode: marketplaceCode,
		Status: PublicationPending, CreatedAt: createdAt,
	})
}

// MarkRunning, işi running durumuna alır.
func (r *ProductPublicationRun) MarkRunning(startedAt time.Time) sharedkernel.Result {
	if r.Status != PublicationPending {
		return sharedkernel.Fail(sharedkernel.NewConflictError("Only pending publication runs can be started."))
	}
	r.Status = PublicationRunning
	r.StartedAt = &startedAt
	return sharedkernel.Ok()
}

// UpdateProgress, ilerleme sayaçlarını günceller.
func (r *ProductPublicationRun) UpdateProgress(processed, published, failed int, total *int) {
	r.ProcessedItems = processed
	r.PublishedItems = published
	r.FailedItems = failed
	r.TotalItems = total
}

// AddError, kalem bazlı hata kaydı ekler; sınır aşıldıysa eklemez ve false döner.
func (r *ProductPublicationRun) AddError(productItemID uuid.UUID, message string) bool {
	if len(r.Errors) >= PublicationMaxErrors {
		return false
	}
	normalized := strings.TrimSpace(message)
	if normalized == "" {
		normalized = "Publication failed."
	}
	if len([]rune(normalized)) > PublicationErrorMessageMaxLength {
		normalized = string([]rune(normalized)[:PublicationErrorMessageMaxLength])
	}
	r.Errors = append(r.Errors, ProductPublicationError{
		ID: uuid.New(), ProductItemID: productItemID, Message: normalized})
	return true
}

// MarkCompleted, işi tamamlar; hata alan kalem varsa completed_with_errors olur.
func (r *ProductPublicationRun) MarkCompleted(completedAt time.Time) sharedkernel.Result {
	if r.Status != PublicationRunning {
		return sharedkernel.Fail(sharedkernel.NewConflictError("Only running publication runs can be completed."))
	}
	if r.FailedItems > 0 {
		r.Status = PublicationCompletedWithErrors
	} else {
		r.Status = PublicationCompleted
	}
	r.CompletedAt = &completedAt
	r.ErrorMessage = nil
	return sharedkernel.Ok()
}

// MarkFailed, işi altyapı hatasıyla sonlandırır.
func (r *ProductPublicationRun) MarkFailed(completedAt time.Time, errorMessage string) sharedkernel.Result {
	if r.Status != PublicationRunning {
		return sharedkernel.Fail(sharedkernel.NewConflictError("Only running publication runs can be marked as failed."))
	}
	r.Status = PublicationFailed
	r.CompletedAt = &completedAt
	message := strings.TrimSpace(errorMessage)
	if message == "" {
		message = "Publication failed."
	}
	r.ErrorMessage = &message
	return sharedkernel.Ok()
}

// IsActive, işin aktif (pending ya da running) olup olmadığını döner.
func (r *ProductPublicationRun) IsActive() bool {
	return r.Status == PublicationPending || r.Status == PublicationRunning
}
