package productimports

import (
	"context"
	"fmt"
	"log/slog"
	"strings"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Bu dosya, claim edilmiş ürün import işini uçtan uca yürüten işlemcidir
// (.NET ProcessProductImportHandler portu).
//
// Ana akış: ürün sayfaları çekilir → kategori attribute cache'i tazelenir →
// planlayıcı ile plan kurulur → kategori zinciri + eksen/özellik tanımları +
// kanal eşlemeleri garanti edilir → grup başına ürün oluşturulur, fiyat tanımı
// tutarları ve görseller yazılır → ilerleme periyodik kaydedilir → run tamamlanır.
//
// Hata durumları: altyapı hataları (bağlantı/istemci) run'ı failed yapar; grup
// düzeyi hatalar run hatası olarak kaydedilip diğer gruplarla devam edilir
// (completed_with_errors). Yalnızca run kaydının kendisi yazılamazsa hata döner.

// Store, işlemcinin ihtiyaç duyduğu Channels kalıcılık yüzeyidir; somut
// karşılığı channels/infrastructure.Repository'dir.
type Store interface {
	// UpdateImportRun, iş kaydını (yeni hata satırlarıyla) kalıcılaştırır.
	UpdateImportRun(ctx context.Context, run *domain.ProductImportRun) error

	// GetConnection, tenant'ın pazaryeri bağlantısını döner; yoksa nil.
	GetConnection(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) (*domain.MarketplaceConnection, error)

	// GetExternalCategory, harici kategori cache kaydını döner; yoksa nil.
	GetExternalCategory(ctx context.Context, marketplaceCode, externalID string) (*domain.ExternalCategory, error)

	// RefreshExternalAttributes, kategorinin özellik+değer cache'ini tazeler.
	RefreshExternalAttributes(ctx context.Context, marketplaceCode, externalCategoryID string, nodes []application.MarketplaceCategoryAttributeNode, syncedAt time.Time) error

	// ListExternalAttributes, kategorinin özellik cache'ini döner.
	ListExternalAttributes(ctx context.Context, marketplaceCode, externalCategoryID string) ([]*domain.ExternalCategoryAttribute, error)

	// GetCategoryMappingByExternalID, harici kimlikle kategori eşlemesini döner; yoksa nil.
	GetCategoryMappingByExternalID(ctx context.Context, tenantID uuid.UUID, marketplaceCode, externalID string) (*domain.CategoryChannelMapping, error)

	// GetCategoryMapping, catalog kategorisiyle eşlemeyi döner; yoksa nil.
	GetCategoryMapping(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID) (*domain.CategoryChannelMapping, error)

	// AddCategoryMapping, yeni kategori eşlemesi ekler.
	AddCategoryMapping(ctx context.Context, tenantID uuid.UUID, m *domain.CategoryChannelMapping) error

	// UpdateCategoryMapping, kategori eşlemesini günceller.
	UpdateCategoryMapping(ctx context.Context, tenantID uuid.UUID, m *domain.CategoryChannelMapping) error

	// RemoveCategoryMapping, kategori eşlemesini siler.
	RemoveCategoryMapping(ctx context.Context, tenantID uuid.UUID, id uuid.UUID) error

	// GetAttributeMapping, doğal anahtarla alan eşlemesini döner; yoksa nil.
	GetAttributeMapping(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID, sourceType domain.AttributeMappingSourceType, catalogSourceID uuid.UUID) (*domain.AttributeChannelMapping, error)

	// AddAttributeMapping, yeni alan eşlemesi ekler.
	AddAttributeMapping(ctx context.Context, tenantID uuid.UUID, m *domain.AttributeChannelMapping) error

	// UpdateAttributeMapping, alan eşlemesini günceller.
	UpdateAttributeMapping(ctx context.Context, tenantID uuid.UUID, m *domain.AttributeChannelMapping) error

	// GetValueMapping, doğal anahtarla değer eşlemesini döner; yoksa nil.
	GetValueMapping(ctx context.Context, tenantID uuid.UUID, attributeMappingID, catalogValueID uuid.UUID) (*domain.AttributeValueChannelMapping, error)

	// AddValueMapping, yeni değer eşlemesi ekler.
	AddValueMapping(ctx context.Context, tenantID uuid.UUID, m *domain.AttributeValueChannelMapping) error

	// UpdateValueMapping, değer eşlemesini günceller.
	UpdateValueMapping(ctx context.Context, tenantID uuid.UUID, m *domain.AttributeValueChannelMapping) error
}

// ListingStore, listeleme tohumlama için gereken kalıcılık yüzeyidir; somut
// karşılığı channels/infrastructure.ListingRepository'dir.
type ListingStore interface {
	// ListByProductItems, kalemlerin bu pazaryerindeki listelemelerini döner.
	ListByProductItems(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, productItemIDs []uuid.UUID) ([]*domain.ProductListing, error)

	// AddRange, yeni listeleme kayıtlarını ekler.
	AddRange(ctx context.Context, listings []*domain.ProductListing) error
}

// Options, işlemcinin çalışma ayarlarıdır (.NET ProductImportOptions karşılığı).
type Options struct {
	// PageSize, pazaryerinden ürün çekerken kullanılan sayfa boyutudur.
	PageSize int

	// ProgressSaveEveryGroups, ilerlemenin kaç grupta bir kaydedileceğidir.
	ProgressSaveEveryGroups int

	// MaxImagesPerProduct, ürün başına aktarılacak en fazla görsel sayısıdır.
	MaxImagesPerProduct int
}

// DefaultOptions, .NET varsayılanlarıyla ayarları döner.
func DefaultOptions() Options {
	return Options{PageSize: 200, ProgressSaveEveryGroups: 1, MaxImagesPerProduct: 8}
}

// Processor, claim edilmiş import işlerini yürüten orkestratördür.
type Processor struct {
	store            Store
	listings         ListingStore
	catalog          CatalogImportGateway
	productsClient   MarketplaceProductsClient
	attributesClient application.CategoryAttributesClient
	options          Options
}

// NewProcessor, bağımlılıklarıyla işlemciyi oluşturur.
func NewProcessor(
	store Store,
	listings ListingStore,
	catalog CatalogImportGateway,
	productsClient MarketplaceProductsClient,
	attributesClient application.CategoryAttributesClient,
	options Options,
) *Processor {
	if options.PageSize < 1 {
		options.PageSize = 1
	}
	if options.ProgressSaveEveryGroups < 1 {
		options.ProgressSaveEveryGroups = 1
	}
	if options.MaxImagesPerProduct < 0 {
		options.MaxImagesPerProduct = 0
	}
	return &Processor{
		store: store, listings: listings, catalog: catalog,
		productsClient: productsClient, attributesClient: attributesClient,
		options: options,
	}
}

// importProgress, run ilerleme sayaçlarıdır.
type importProgress struct {
	total     int
	processed int
	imported  int
	skipped   int
	failed    int
}

// categorySetup, garanti edilmiş kategori kurulumudur.
type categorySetup struct {
	catalogCategoryID  uuid.UUID
	externalCategoryID string
}

// ensuredAxis, garanti edilmiş varyant ekseninin run içi önbelleğidir.
type ensuredAxis struct {
	variantID      uuid.UUID
	isColor        bool
	slicer         bool
	valueIDByLabel map[string]uuid.UUID // küçük harf etiket → değer kimliği
}

// ensuredAttribute, garanti edilmiş özelliğin run içi önbelleğidir.
type ensuredAttribute struct {
	attributeID   uuid.UUID
	valueIDByName map[string]uuid.UUID // küçük harf ad → değer kimliği
}

// priceDefinitionPair, run başına bir kez garanti edilen fiyat tanımlarıdır.
type priceDefinitionPair struct {
	saleDefinitionID    uuid.UUID
	compareDefinitionID uuid.UUID
}

// assignmentKey, kategori+özellik atama önbelleğinin anahtarıdır.
type assignmentKey struct {
	categoryID  uuid.UUID
	attributeID uuid.UUID
}

// mappingKey, alan eşlemesi önbelleğinin anahtarıdır.
type mappingKey struct {
	categoryID uuid.UUID
	sourceType domain.AttributeMappingSourceType
	sourceID   uuid.UUID
}

// valueMappingKey, değer eşlemesi önbelleğinin anahtarıdır.
type valueMappingKey struct {
	mappingID      uuid.UUID
	catalogValueID uuid.UUID
}

// importContext, run boyunca yaşayan garanti önbellekleridir
// (.NET ImportContext karşılığı).
type importContext struct {
	marketplaceCode     string
	tenantID            uuid.UUID
	categories          map[string]categorySetup
	axesByName          map[string]*ensuredAxis      // küçük harf eksen adı
	attributesByName    map[string]*ensuredAttribute // küçük harf özellik adı
	assignedAttributes  map[assignmentKey]PlannedAttributeScope
	attributeMappingIDs map[mappingKey]uuid.UUID
	mappedValues        map[valueMappingKey]struct{}
	priceDefinitions    *priceDefinitionPair
}

// newImportContext, boş önbelleklerle bağlamı kurar.
func newImportContext(marketplaceCode string, tenantID uuid.UUID) *importContext {
	return &importContext{
		marketplaceCode:     marketplaceCode,
		tenantID:            tenantID,
		categories:          map[string]categorySetup{},
		axesByName:          map[string]*ensuredAxis{},
		attributesByName:    map[string]*ensuredAttribute{},
		assignedAttributes:  map[assignmentKey]PlannedAttributeScope{},
		attributeMappingIDs: map[mappingKey]uuid.UUID{},
		mappedValues:        map[valueMappingKey]struct{}{},
	}
}

// Process, running durumundaki import işini uçtan uca yürütür. Yalnızca run
// kaydının yazılamaması hata döner; pazaryeri/grup hataları run'a işlenir.
func (p *Processor) Process(ctx context.Context, run *domain.ProductImportRun) error {
	if run.Status != domain.ImportRunning {
		return fmt.Errorf("productimports: iş running durumunda değil: %s", run.Status)
	}

	connection, err := p.store.GetConnection(ctx, run.TenantID, run.MarketplaceCode)
	if err != nil {
		return err
	}
	if connection == nil || !connection.IsEnabled {
		return p.failRun(ctx, run, "Marketplace connection is missing or disabled.")
	}
	credentials := &application.MarketplaceCredentials{
		SellerID: connection.SellerID, ApiKey: connection.ApiKey, ApiSecret: connection.ApiSecret}

	rows, fetchErr := p.fetchAllProducts(ctx, credentials)
	if fetchErr != nil {
		return p.failRun(ctx, run, fetchErr.Message)
	}

	defsByCategory := p.buildAttributeDefs(ctx, credentials, run.MarketplaceCode, rows)
	plan := BuildPlan(rows, defsByCategory)

	progress := importProgress{total: len(plan.Groups)}
	total := len(plan.Groups)
	run.UpdateProgress(0, 0, 0, 0, &total)
	if err := p.store.UpdateImportRun(ctx, run); err != nil {
		return err
	}

	gctx := newImportContext(run.MarketplaceCode, run.TenantID)
	for _, group := range plan.Groups {
		if ctx.Err() != nil {
			return ctx.Err()
		}
		p.importGroup(ctx, run, gctx, group, &progress)
		progress.processed++
		if progress.processed%p.options.ProgressSaveEveryGroups == 0 {
			applyProgress(run, progress)
			if err := p.store.UpdateImportRun(ctx, run); err != nil {
				return err
			}
		}
	}

	applyProgress(run, progress)
	if completeResult := run.MarkCompleted(time.Now().UTC()); completeResult.IsFailure() {
		return fmt.Errorf("productimports: iş tamamlanamadı: %s", completeResult.Err().Message)
	}
	if err := p.store.UpdateImportRun(ctx, run); err != nil {
		return err
	}
	slog.Info("Product import finished.",
		slog.String("RunId", run.ID.String()),
		slog.String("TenantId", run.TenantID.String()),
		slog.Int("Imported", progress.imported),
		slog.Int("Skipped", progress.skipped),
		slog.Int("Failed", progress.failed))
	return nil
}

// failRun, işi altyapı hatasıyla sonlandırır ve kaydeder.
func (p *Processor) failRun(ctx context.Context, run *domain.ProductImportRun, message string) error {
	slog.Error("Product import failed.",
		slog.String("RunId", run.ID.String()),
		slog.String("TenantId", run.TenantID.String()),
		slog.String("Error", message))
	if failResult := run.MarkFailed(time.Now().UTC(), message); failResult.IsFailure() {
		return nil
	}
	return p.store.UpdateImportRun(ctx, run)
}

// applyProgress, sayaçları run'a yazar.
func applyProgress(run *domain.ProductImportRun, progress importProgress) {
	total := progress.total
	run.UpdateProgress(progress.processed, progress.imported, progress.skipped, progress.failed, &total)
}

// fetchAllProducts, tüm ürün sayfalarını sırayla çekip birleştirir
// (.NET FetchAllProductsAsync portu).
func (p *Processor) fetchAllProducts(ctx context.Context, credentials *application.MarketplaceCredentials) ([]MarketplaceProductNode, *sharedkernel.Error) {
	rows := []MarketplaceProductNode{}
	page := 0
	for {
		if ctx.Err() != nil {
			return nil, sharedkernel.NewFailureError("İçe aktarma iptal edildi: " + ctx.Err().Error())
		}
		pageResult := p.productsClient.FetchProductsPage(ctx, credentials, page, p.options.PageSize)
		if pageResult.IsFailure() {
			return nil, pageResult.Err()
		}
		value := pageResult.Value()
		rows = append(rows, value.Items...)
		page++
		if page >= value.TotalPages || len(value.Items) == 0 {
			return rows, nil
		}
	}
}

// buildAttributeDefs, satırlarda geçen her dış kategori için attribute cache'ini
// tazeleyip planlayıcı tanımlarını kurar; tazeleme başarısızsa cache'e düşer
// (.NET BuildAttributeDefsAsync portu).
func (p *Processor) buildAttributeDefs(
	ctx context.Context,
	credentials *application.MarketplaceCredentials,
	marketplaceCode string,
	rows []MarketplaceProductNode,
) map[string][]ProductImportAttributeDef {
	defsByCategory := map[string][]ProductImportAttributeDef{}
	for _, row := range rows {
		externalCategoryID := row.ExternalCategoryID
		if strings.TrimSpace(externalCategoryID) == "" {
			continue
		}
		if _, seen := defsByCategory[externalCategoryID]; seen {
			continue
		}

		fetchResult := p.attributesClient.FetchCategoryAttributes(ctx, credentials, externalCategoryID)
		if fetchResult.IsFailure() {
			slog.Warn("Category attribute cache refresh failed; falling back to cached definitions.",
				slog.String("ExternalCategoryId", externalCategoryID),
				slog.String("Error", fetchResult.Err().Message))
		} else if err := p.store.RefreshExternalAttributes(ctx, marketplaceCode, externalCategoryID, fetchResult.Value(), time.Now().UTC()); err != nil {
			slog.Warn("Category attribute cache refresh failed; falling back to cached definitions.",
				slog.String("ExternalCategoryId", externalCategoryID),
				slog.String("Error", err.Error()))
		}

		cached, err := p.store.ListExternalAttributes(ctx, marketplaceCode, externalCategoryID)
		if err != nil {
			slog.Warn("Cached category attributes could not be read.",
				slog.String("ExternalCategoryId", externalCategoryID),
				slog.String("Error", err.Error()))
			defsByCategory[externalCategoryID] = nil
			continue
		}
		defs := make([]ProductImportAttributeDef, len(cached))
		for i, attribute := range cached {
			defs[i] = ProductImportAttributeDef{
				ExternalAttributeID: attribute.ExternalAttributeID,
				Name:                attribute.Name,
				Required:            attribute.Required,
				AllowCustom:         attribute.AllowCustom,
				IsVariant:           attribute.IsVariant,
				IsSlicer:            attribute.IsSlicer,
			}
		}
		defsByCategory[externalCategoryID] = defs
	}
	return defsByCategory
}

// importGroup, tek ürün grubunu içe aktarır; hataları run'a işler
// (.NET ImportGroupAsync portu).
func (p *Processor) importGroup(
	ctx context.Context,
	run *domain.ProductImportRun,
	gctx *importContext,
	group ProductGroupPlan,
	progress *importProgress,
) {
	for _, warning := range group.Warnings {
		run.AddError(group.ProductMainID, nil, "Uyarı: "+warning)
	}
	if group.Error != nil {
		progress.failed++
		run.AddError(group.ProductMainID, nil, *group.Error)
		return
	}

	categorySetupResult := p.ensureCategorySetup(ctx, gctx, group.ExternalCategoryID)
	if categorySetupResult.IsFailure() {
		progress.failed++
		run.AddError(group.ProductMainID, nil, categorySetupResult.Err().Message)
		return
	}
	setup := categorySetupResult.Value()

	barcodes := make([]string, len(group.Items))
	for i, item := range group.Items {
		barcodes[i] = item.Barcode
	}
	exists, err := p.catalog.ProductGroupExists(ctx, gctx.tenantID, group.ModelCode, barcodes)
	if err != nil {
		progress.failed++
		run.AddError(group.ProductMainID, nil, err.Error())
		return
	}
	if exists {
		// Grup zaten kataloğa alınmış; ürünü yeniden oluşturmayız ama listeleme
		// kaydı eksik olabilir (bu özellikten önce import edilmiş kalemler).
		// Import'un yeniden çalıştırılması böylece listing backfill'i olarak da
		// işe yarar.
		p.seedListingsForExistingGroup(ctx, gctx, barcodes, run, group)
		progress.skipped++
		return
	}

	buildResult := p.buildBatchInput(ctx, gctx, setup, group)
	if buildResult.IsFailure() {
		progress.failed++
		run.AddError(group.ProductMainID, nil, buildResult.Err().Message)
		return
	}

	createResult := p.catalog.CreateProductsBatch(ctx, gctx.tenantID, buildResult.Value())
	if createResult.IsFailure() {
		progress.failed++
		run.AddError(group.ProductMainID, nil, createResult.Err().Message)
		return
	}
	created := createResult.Value()

	p.writeItemPrices(ctx, gctx, group, created, run)
	p.attachImages(ctx, gctx, group, created, run)
	p.seedListingsForCreatedGroup(ctx, gctx, created, run, group)
	progress.imported++
}

// ensureCategorySetup, dış kategoriyi catalog kategorisine bağlar
// (.NET EnsureCategorySetupAsync portu). Dedup önce EŞLEME üzerinden yapılır:
// bu dış kategori daha önce bir catalog kategorisine eşlendiyse, kullanıcı
// kategoriyi ağaçta taşımış/yeniden adlandırmış olsa bile aynı kategori
// yeniden kullanılır (mükerrer "Halı" oluşmaz).
func (p *Processor) ensureCategorySetup(ctx context.Context, gctx *importContext, externalCategoryID string) sharedkernel.ResultOf[categorySetup] {
	if cached, ok := gctx.categories[externalCategoryID]; ok {
		return sharedkernel.OkOf(cached)
	}
	externalCategory, err := p.store.GetExternalCategory(ctx, gctx.marketplaceCode, externalCategoryID)
	if err != nil {
		return sharedkernel.FailOf[categorySetup](sharedkernel.NewInternalError(err.Error()))
	}
	if externalCategory == nil {
		return sharedkernel.FailOf[categorySetup](sharedkernel.NewNotFoundError(fmt.Sprintf(
			"Pazaryeri kategorisi cache'te yok (id: %s). Önce kategori senkronizasyonu çalıştırılmalı.",
			externalCategoryID)))
	}

	existingMapping, err := p.store.GetCategoryMappingByExternalID(ctx, gctx.tenantID, gctx.marketplaceCode, externalCategoryID)
	if err != nil {
		return sharedkernel.FailOf[categorySetup](sharedkernel.NewInternalError(err.Error()))
	}
	if existingMapping != nil {
		categoryExists, err := p.catalog.CategoryExists(ctx, gctx.tenantID, existingMapping.CatalogCategoryID)
		if err != nil {
			return sharedkernel.FailOf[categorySetup](sharedkernel.NewInternalError(err.Error()))
		}
		if categoryExists {
			setup := categorySetup{catalogCategoryID: existingMapping.CatalogCategoryID, externalCategoryID: externalCategoryID}
			gctx.categories[externalCategoryID] = setup
			return sharedkernel.OkOf(setup)
		}
		// Eşlenen kategori kullanıcı tarafından silinmiş: ölü eşlemeyi temizle,
		// aşağıda yenisi kurulur.
		if err := p.store.RemoveCategoryMapping(ctx, gctx.tenantID, existingMapping.ID); err != nil {
			return sharedkernel.FailOf[categorySetup](sharedkernel.NewInternalError(err.Error()))
		}
	}

	// Eşleme yoksa (ya da eşlenen kategori silinmişse): Shopify koleksiyonu gibi
	// DÜZ model — yalnızca yaprak kategori oluşturulur; Trendyol tarafındaki tam
	// yol eşlemede (ExternalCategory.Path) zaten bilinir, üst zincir Pimly
	// ağacına kopyalanmaz.
	leafResult := p.catalog.EnsureCategoryPath(ctx, gctx.tenantID, []string{externalCategory.Name})
	if leafResult.IsFailure() {
		return sharedkernel.FailOf[categorySetup](leafResult.Err())
	}
	if err := p.upsertCategoryMapping(ctx, gctx, leafResult.Value(), externalCategoryID); err != nil {
		return sharedkernel.FailOf[categorySetup](sharedkernel.NewInternalError(err.Error()))
	}
	setup := categorySetup{catalogCategoryID: leafResult.Value(), externalCategoryID: externalCategoryID}
	gctx.categories[externalCategoryID] = setup
	return sharedkernel.OkOf(setup)
}

// upsertCategoryMapping, kategori eşlemesini ekler ya da harici kimliğini günceller.
func (p *Processor) upsertCategoryMapping(ctx context.Context, gctx *importContext, catalogCategoryID uuid.UUID, externalCategoryID string) error {
	existing, err := p.store.GetCategoryMapping(ctx, gctx.tenantID, gctx.marketplaceCode, catalogCategoryID)
	if err != nil {
		return err
	}
	if existing != nil {
		if existing.ExternalID != externalCategoryID {
			existing.ExternalID = externalCategoryID
			return p.store.UpdateCategoryMapping(ctx, gctx.tenantID, existing)
		}
		return nil
	}
	return p.store.AddCategoryMapping(ctx, gctx.tenantID, &domain.CategoryChannelMapping{
		ID: uuid.New(), CatalogCategoryID: catalogCategoryID,
		MarketplaceCode: gctx.marketplaceCode, ExternalID: externalCategoryID,
	})
}

// buildBatchInput, grubun toplu oluşturma girdisini kurar; eksen/özellik/marka
// garanti çağrılarını yapar (.NET BuildBatchInputAsync portu).
func (p *Processor) buildBatchInput(
	ctx context.Context,
	gctx *importContext,
	setup categorySetup,
	group ProductGroupPlan,
) sharedkernel.ResultOf[CatalogProductBatchInput] {
	// Marka: ada göre tenant içinde idempotent garanti edilir; başarısız olsa
	// bile (marka opsiyonel olduğundan) import'u bozmaz, yalnızca ürün markasız kalır.
	var brandID *uuid.UUID
	if group.BrandName != nil {
		brandResult := p.catalog.EnsureBrand(ctx, gctx.tenantID, *group.BrandName, group.BrandExternalID)
		if brandResult.IsSuccess() {
			id := brandResult.Value()
			brandID = &id
		}
	}

	// Varyant eksenleri: global (tenant) düzeyde ada göre tekilleştirilir.
	axisInputs := []CatalogVariantAxisInput{}
	axisByExternalID := map[string]*ensuredAxis{}
	for _, axis := range group.VariantAxes {
		ensured := p.ensureAxis(ctx, gctx, setup, axis)
		if ensured.IsFailure() {
			return sharedkernel.FailOf[CatalogProductBatchInput](ensured.Err())
		}
		value := ensured.Value()
		axisByExternalID[axis.ExternalAttributeID] = value
		axisInputs = append(axisInputs, CatalogVariantAxisInput{
			VariantID: value.variantID, IsColor: value.isColor, Slicer: value.slicer})
	}

	// Ürün düzeyi özellik değerleri.
	attributeSelections := []CatalogSelectionInput{}
	for _, attributeValue := range group.AttributeValues {
		selection := p.ensureAttributeSelection(ctx, gctx, setup, attributeValue)
		if selection.IsFailure() {
			return sharedkernel.FailOf[CatalogProductBatchInput](selection.Err())
		}
		attributeSelections = append(attributeSelections, selection.Value())
	}

	// Kalemler.
	itemInputs := []CatalogProductItemInput{}
	for _, item := range group.Items {
		variantSelections := []CatalogSelectionInput{}
		for _, selection := range item.VariantSelections {
			axis := axisByExternalID[selection.ExternalAttributeID]
			valueResult := p.ensureAxisValue(ctx, gctx, axis, selection)
			if valueResult.IsFailure() {
				return sharedkernel.FailOf[CatalogProductBatchInput](valueResult.Err())
			}
			variantSelections = append(variantSelections, CatalogSelectionInput{
				ID: axis.variantID, ValueID: valueResult.Value()})
		}
		itemAttributeSelections := []CatalogSelectionInput{}
		for _, attributeValue := range item.ItemAttributeValues {
			selection := p.ensureAttributeSelection(ctx, gctx, setup, attributeValue)
			if selection.IsFailure() {
				return sharedkernel.FailOf[CatalogProductBatchInput](selection.Err())
			}
			itemAttributeSelections = append(itemAttributeSelections, selection.Value())
		}
		itemInputs = append(itemInputs, CatalogProductItemInput{
			Sku: item.Sku, Barcode: item.Barcode,
			Price: item.Price, CompareAtPrice: item.CompareAtPrice,
			Stock: item.Stock, Currency: item.Currency,
			VariantValues: variantSelections, AttributeValues: itemAttributeSelections,
		})
	}

	// Split (renk) başına geçersiz kılmalar + renk-bazlı özellik seçimleri.
	splitInputs := []CatalogSplitInput{}
	for _, split := range group.SplitOverrides {
		splitSelections := []CatalogSelectionInput{}
		for _, attributeValue := range split.SplitAttributeValues {
			selection := p.ensureAttributeSelection(ctx, gctx, setup, attributeValue)
			if selection.IsFailure() {
				return sharedkernel.FailOf[CatalogProductBatchInput](selection.Err())
			}
			splitSelections = append(splitSelections, selection.Value())
		}
		splitInputs = append(splitInputs, CatalogSplitInput{
			ValueName: split.ValueName, ModelCode: split.StockCode,
			Name: split.Title, Description: split.Description,
			AttributeValues: splitSelections,
		})
	}

	return sharedkernel.OkOf(CatalogProductBatchInput{
		GroupID:    uuid.New(),
		CategoryID: setup.catalogCategoryID,
		ModelCode:  group.ModelCode,
		Name:       group.Name,
		Status:     "active",
		AttributeValues: attributeSelections,
		Variants:        axisInputs,
		Items:           itemInputs,
		Splits:          splitInputs,
		BrandID:         brandID,
		Description:     group.Description,
	})
}

// ensureAxis, varyant eksenini garanti eder ve eşlemesini yazar
// (.NET EnsureAxisAsync portu).
func (p *Processor) ensureAxis(ctx context.Context, gctx *importContext, setup categorySetup, axis PlannedVariantAxis) sharedkernel.ResultOf[*ensuredAxis] {
	nameKey := strings.ToLower(axis.Name)
	ensured, ok := gctx.axesByName[nameKey]
	if !ok {
		variantResult := p.catalog.EnsureVariant(ctx, gctx.tenantID, axis.Name, axis.IsColor, axis.Slicer)
		if variantResult.IsFailure() {
			return sharedkernel.FailOf[*ensuredAxis](variantResult.Err())
		}
		snapshot := variantResult.Value()
		ensured = &ensuredAxis{
			variantID: snapshot.ID, isColor: snapshot.IsColor, slicer: snapshot.Slicer,
			valueIDByLabel: map[string]uuid.UUID{},
		}
		gctx.axesByName[nameKey] = ensured
	}
	if err := p.upsertAttributeMapping(ctx, gctx, setup, domain.SourceCatalogVariant, ensured.variantID, axis.ExternalAttributeID); err != nil {
		return sharedkernel.FailOf[*ensuredAxis](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(ensured)
}

// ensureAxisValue, eksen değerini garanti eder ve değer eşlemesini yazar
// (.NET EnsureAxisValueAsync portu).
func (p *Processor) ensureAxisValue(ctx context.Context, gctx *importContext, axis *ensuredAxis, selection PlannedVariantSelection) sharedkernel.ResultOf[uuid.UUID] {
	labelKey := strings.ToLower(selection.ValueName)
	valueID, ok := axis.valueIDByLabel[labelKey]
	if !ok {
		valueResult := p.catalog.EnsureVariantValue(ctx, gctx.tenantID, axis.variantID, selection.ValueName)
		if valueResult.IsFailure() {
			return valueResult
		}
		valueID = valueResult.Value()
		axis.valueIDByLabel[labelKey] = valueID
	}
	if selection.ExternalValueID != nil {
		if err := p.upsertValueMapping(ctx, gctx, axis.variantID, valueID, *selection.ExternalValueID); err != nil {
			return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewInternalError(err.Error()))
		}
	}
	return sharedkernel.OkOf(valueID)
}

// ensureAttributeSelection, özelliği + değerini + kategori atamasını + kanal
// eşlemelerini garanti eder (.NET EnsureAttributeSelectionAsync portu).
func (p *Processor) ensureAttributeSelection(
	ctx context.Context,
	gctx *importContext,
	setup categorySetup,
	attributeValue PlannedAttributeValue,
) sharedkernel.ResultOf[CatalogSelectionInput] {
	nameKey := strings.ToLower(attributeValue.AttributeName)
	ensured, ok := gctx.attributesByName[nameKey]
	if !ok {
		attributeResult := p.catalog.EnsureAttribute(ctx, gctx.tenantID, attributeValue.AttributeName)
		if attributeResult.IsFailure() {
			return sharedkernel.FailOf[CatalogSelectionInput](attributeResult.Err())
		}
		ensured = &ensuredAttribute{
			attributeID: attributeResult.Value(), valueIDByName: map[string]uuid.UUID{}}
		gctx.attributesByName[nameKey] = ensured
	}

	// Atama run başına bir kez yapılır; ancak model seviyesinde atanmış bir
	// özellik için sonradan daha özgül bir seviye (slicer/kalem) tespit
	// edilirse yükseltme yeniden gönderilir.
	key := assignmentKey{categoryID: setup.catalogCategoryID, attributeID: ensured.attributeID}
	assignedScope, hasAssigned := gctx.assignedAttributes[key]
	if !hasAssigned || (assignedScope == ScopeModel && attributeValue.Scope != ScopeModel) {
		assignResult := p.catalog.AssignAttributeToCategory(
			ctx, gctx.tenantID, setup.catalogCategoryID, ensured.attributeID,
			attributeValue.Required, len(gctx.assignedAttributes)+1, attributeValue.Scope)
		if assignResult.IsFailure() {
			return sharedkernel.FailOf[CatalogSelectionInput](assignResult.Err())
		}
		gctx.assignedAttributes[key] = attributeValue.Scope
	}

	if err := p.upsertAttributeMapping(ctx, gctx, setup, domain.SourceCatalogAttribute, ensured.attributeID, attributeValue.ExternalAttributeID); err != nil {
		return sharedkernel.FailOf[CatalogSelectionInput](sharedkernel.NewInternalError(err.Error()))
	}

	valueKey := strings.ToLower(attributeValue.ValueName)
	valueID, ok := ensured.valueIDByName[valueKey]
	if !ok {
		valueResult := p.catalog.EnsureAttributeValue(ctx, gctx.tenantID, ensured.attributeID, attributeValue.ValueName)
		if valueResult.IsFailure() {
			return sharedkernel.FailOf[CatalogSelectionInput](valueResult.Err())
		}
		valueID = valueResult.Value()
		ensured.valueIDByName[valueKey] = valueID
	}
	if attributeValue.ExternalValueID != nil {
		if err := p.upsertValueMapping(ctx, gctx, ensured.attributeID, valueID, *attributeValue.ExternalValueID); err != nil {
			return sharedkernel.FailOf[CatalogSelectionInput](sharedkernel.NewInternalError(err.Error()))
		}
	}
	return sharedkernel.OkOf(CatalogSelectionInput{ID: ensured.attributeID, ValueID: valueID})
}

// upsertAttributeMapping, alan eşlemesini garanti eder; kimliği bağlamda
// önbelleklenir (.NET UpsertAttributeMappingAsync portu).
func (p *Processor) upsertAttributeMapping(
	ctx context.Context,
	gctx *importContext,
	setup categorySetup,
	sourceType domain.AttributeMappingSourceType,
	catalogSourceID uuid.UUID,
	externalAttributeID string,
) error {
	key := mappingKey{categoryID: setup.catalogCategoryID, sourceType: sourceType, sourceID: catalogSourceID}
	if _, exists := gctx.attributeMappingIDs[key]; exists {
		return nil
	}
	existing, err := p.store.GetAttributeMapping(ctx, gctx.tenantID, gctx.marketplaceCode, setup.catalogCategoryID, sourceType, catalogSourceID)
	if err != nil {
		return err
	}
	if existing != nil {
		if existing.ExternalAttributeID != externalAttributeID {
			existing.ExternalAttributeID = externalAttributeID
			if err := p.store.UpdateAttributeMapping(ctx, gctx.tenantID, existing); err != nil {
				return err
			}
		}
		gctx.attributeMappingIDs[key] = existing.ID
		return nil
	}
	mapping := &domain.AttributeChannelMapping{
		ID: uuid.New(), MarketplaceCode: gctx.marketplaceCode,
		CatalogCategoryID: setup.catalogCategoryID, SourceType: sourceType,
		CatalogSourceID: catalogSourceID, ExternalAttributeID: externalAttributeID,
	}
	if err := p.store.AddAttributeMapping(ctx, gctx.tenantID, mapping); err != nil {
		return err
	}
	gctx.attributeMappingIDs[key] = mapping.ID
	return nil
}

// upsertValueMapping, değer eşlemesini garanti eder
// (.NET UpsertValueMappingAsync portu).
func (p *Processor) upsertValueMapping(
	ctx context.Context,
	gctx *importContext,
	catalogSourceID uuid.UUID,
	catalogValueID uuid.UUID,
	externalValueID string,
) error {
	var mappingID *uuid.UUID
	for key, id := range gctx.attributeMappingIDs {
		if key.sourceID == catalogSourceID {
			value := id
			mappingID = &value
			break
		}
	}
	if mappingID == nil {
		return nil
	}
	valueKey := valueMappingKey{mappingID: *mappingID, catalogValueID: catalogValueID}
	if _, exists := gctx.mappedValues[valueKey]; exists {
		return nil
	}
	gctx.mappedValues[valueKey] = struct{}{}

	existing, err := p.store.GetValueMapping(ctx, gctx.tenantID, *mappingID, catalogValueID)
	if err != nil {
		return err
	}
	if existing != nil {
		if existing.ExternalValueID != externalValueID {
			existing.ExternalValueID = externalValueID
			return p.store.UpdateValueMapping(ctx, gctx.tenantID, existing)
		}
		return nil
	}
	return p.store.AddValueMapping(ctx, gctx.tenantID, &domain.AttributeValueChannelMapping{
		ID: uuid.New(), AttributeChannelMappingID: *mappingID,
		CatalogValueID: catalogValueID, ExternalValueID: externalValueID,
	})
}

// writeItemPrices, kalemlerin pazaryerine özgü satış/karşılaştırma fiyat tanımı
// tutarlarını yazar (.NET WriteItemPricesAsync portu).
func (p *Processor) writeItemPrices(
	ctx context.Context,
	gctx *importContext,
	group ProductGroupPlan,
	created []CreatedProductSnapshot,
	run *domain.ProductImportRun,
) {
	definitions, err := p.ensurePriceDefinitions(ctx, gctx)
	if err != nil {
		run.AddError(group.ProductMainID, nil, "Fiyat tanımı oluşturulamadı: "+err.Message)
		return
	}
	itemsByBarcode := map[string]PlannedItem{}
	for _, item := range group.Items {
		itemsByBarcode[strings.ToLower(item.Barcode)] = item
	}
	for _, product := range created {
		for barcode, itemID := range product.ItemIDByBarcode {
			plannedItem, ok := itemsByBarcode[strings.ToLower(barcode)]
			if !ok {
				continue
			}
			saleResult := p.catalog.UpsertItemPrice(ctx, gctx.tenantID, itemID,
				definitions.saleDefinitionID, plannedItem.Price, plannedItem.Currency)
			if saleResult.IsFailure() {
				barcodeCopy := barcode
				run.AddError(group.ProductMainID, &barcodeCopy,
					"Satış fiyatı yazılamadı: "+saleResult.Err().Message)
			}
			if plannedItem.CompareAtPrice == nil {
				continue
			}
			compareResult := p.catalog.UpsertItemPrice(ctx, gctx.tenantID, itemID,
				definitions.compareDefinitionID, *plannedItem.CompareAtPrice, plannedItem.Currency)
			if compareResult.IsFailure() {
				barcodeCopy := barcode
				run.AddError(group.ProductMainID, &barcodeCopy,
					"Karşılaştırma fiyatı yazılamadı: "+compareResult.Err().Message)
			}
		}
	}
}

// ensurePriceDefinitions, pazaryerine özgü satış/karşılaştırma fiyat
// tanımlarını run başına bir kez garanti eder (ör. TY → "TY Satış"/"ty_sale"
// ve "TY Karşılaştırma"/"ty_compare"); sonuç bağlamda önbelleklenir.
func (p *Processor) ensurePriceDefinitions(ctx context.Context, gctx *importContext) (*priceDefinitionPair, *sharedkernel.Error) {
	if gctx.priceDefinitions != nil {
		return gctx.priceDefinitions, nil
	}
	namePrefix := strings.ToUpper(gctx.marketplaceCode)
	codePrefix := strings.ToLower(gctx.marketplaceCode)

	saleCode := codePrefix + "_sale"
	saleResult := p.catalog.EnsurePriceDefinition(ctx, gctx.tenantID, namePrefix+" Satış", &saleCode)
	if saleResult.IsFailure() {
		return nil, saleResult.Err()
	}
	compareCode := codePrefix + "_compare"
	compareResult := p.catalog.EnsurePriceDefinition(ctx, gctx.tenantID, namePrefix+" Karşılaştırma", &compareCode)
	if compareResult.IsFailure() {
		return nil, compareResult.Err()
	}
	gctx.priceDefinitions = &priceDefinitionPair{
		saleDefinitionID: saleResult.Value(), compareDefinitionID: compareResult.Value()}
	return gctx.priceDefinitions, nil
}

// attachImages, ürün başına ilk barkodun satırındaki görselleri aktarır
// (renk bölünmesinde renk görselleri; .NET AttachImagesAsync portu).
func (p *Processor) attachImages(
	ctx context.Context,
	gctx *importContext,
	group ProductGroupPlan,
	created []CreatedProductSnapshot,
	run *domain.ProductImportRun,
) {
	maxImages := p.options.MaxImagesPerProduct
	if maxImages == 0 {
		return
	}
	for _, product := range created {
		firstBarcode := ""
		for _, item := range group.Items {
			if _, ok := product.ItemIDByBarcode[item.Barcode]; ok {
				firstBarcode = item.Barcode
				break
			}
		}
		if firstBarcode == "" {
			continue
		}
		var plannedItem *PlannedItem
		for i := range group.Items {
			if strings.EqualFold(group.Items[i].Barcode, firstBarcode) {
				plannedItem = &group.Items[i]
				break
			}
		}
		if plannedItem == nil || len(plannedItem.ImageURLs) == 0 {
			continue
		}
		urls := plannedItem.ImageURLs
		if len(urls) > maxImages {
			urls = urls[:maxImages]
		}
		if result := p.catalog.AddProductImages(ctx, gctx.tenantID, product.ProductID, urls); result.IsFailure() {
			barcodeCopy := firstBarcode
			run.AddError(group.ProductMainID, &barcodeCopy,
				"Görsel aktarılamadı: "+result.Err().Message)
		}
	}
}

// seedListingsForCreatedGroup, bu turda oluşturulan kalemler için listeleme
// kaydı açar. Kalem kimlikleri yeni üretildiği için mevcut kayıt araması yapılmaz.
func (p *Processor) seedListingsForCreatedGroup(
	ctx context.Context,
	gctx *importContext,
	created []CreatedProductSnapshot,
	run *domain.ProductImportRun,
	group ProductGroupPlan,
) {
	itemIDByBarcode := map[string]uuid.UUID{}
	for _, product := range created {
		for barcode, itemID := range product.ItemIDByBarcode {
			itemIDByBarcode[barcode] = itemID
		}
	}
	p.seedListings(ctx, gctx, itemIDByBarcode, false, run, group)
}

// seedListingsForExistingGroup, zaten kataloğa alınmış bir grup için eksik
// listeleme kayıtlarını tamamlar (backfill); mevcut kayıtlara dokunulmaz.
func (p *Processor) seedListingsForExistingGroup(
	ctx context.Context,
	gctx *importContext,
	barcodes []string,
	run *domain.ProductImportRun,
	group ProductGroupPlan,
) {
	itemIDByBarcode, err := p.catalog.ResolveItemIDsByBarcode(ctx, gctx.tenantID, barcodes)
	if err != nil {
		run.AddError(group.ProductMainID, nil, "Listeleme kayıtları çözülemedi: "+err.Error())
		return
	}
	p.seedListings(ctx, gctx, itemIDByBarcode, true, run, group)
}

// seedListings, kalem başına listeleme tohumlar (.NET SeedListingsAsync portu).
// Barkod, kalemin pazaryerindeki listeleme kimliğidir (Trendyol fiyat/stok
// uçları barkodla çalışır): import edilen kalem pazaryerinde hâlihazırda canlıdır.
func (p *Processor) seedListings(
	ctx context.Context,
	gctx *importContext,
	itemIDByBarcode map[string]uuid.UUID,
	lookUpExisting bool,
	run *domain.ProductImportRun,
	group ProductGroupPlan,
) {
	if len(itemIDByBarcode) == 0 {
		return
	}
	alreadyListed := map[uuid.UUID]struct{}{}
	if lookUpExisting {
		itemIDs := make([]uuid.UUID, 0, len(itemIDByBarcode))
		for _, itemID := range itemIDByBarcode {
			itemIDs = append(itemIDs, itemID)
		}
		existing, err := p.listings.ListByProductItems(ctx, gctx.tenantID, gctx.marketplaceCode, itemIDs)
		if err != nil {
			run.AddError(group.ProductMainID, nil, "Mevcut listelemeler okunamadı: "+err.Error())
			return
		}
		for _, listing := range existing {
			alreadyListed[listing.ProductItemID] = struct{}{}
		}
	}
	now := time.Now().UTC()
	seeded := []*domain.ProductListing{}
	for barcode, itemID := range itemIDByBarcode {
		if _, listed := alreadyListed[itemID]; listed {
			continue
		}
		seedResult := domain.SeedListing(gctx.tenantID, gctx.marketplaceCode, itemID, barcode, now)
		if seedResult.IsFailure() {
			barcodeCopy := barcode
			run.AddError(group.ProductMainID, &barcodeCopy,
				"Listeleme kaydı oluşturulamadı: "+seedResult.Err().Message)
			continue
		}
		seeded = append(seeded, seedResult.Value())
	}
	if len(seeded) == 0 {
		return
	}
	if err := p.listings.AddRange(ctx, seeded); err != nil {
		run.AddError(group.ProductMainID, nil, "Listeleme kayıtları yazılamadı: "+err.Error())
	}
}
