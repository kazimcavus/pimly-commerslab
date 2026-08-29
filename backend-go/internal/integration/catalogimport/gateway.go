// Package catalogimport, ürün import hattının Catalog yazma kapısıdır
// (.NET Pimly.Integration/CatalogImportGateway portu). Catalog, Pricing,
// Inventory ve Media handler'larına delege eder; tüm işlemler idempotenttir ve
// tenant her çağrıda açık parametredir.
//
// Go dönemi iyileştirmesi: harici görsel indirme .NET'teki gibi sıralı değil,
// sınırlı eşzamanlılıkla (4 paralel indirme) yapılır; sıra ve birincil görsel
// semantiği korunur.
package catalogimport

import (
	"bytes"
	"context"
	"errors"
	"fmt"
	"io"
	"net/http"
	"strings"
	"sync"
	"time"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5/pgxpool"

	catalogapp "pimly.commerslab/backend-go/internal/modules/catalog/application"
	"pimly.commerslab/backend-go/internal/modules/catalog/domain/categories"
	"pimly.commerslab/backend-go/internal/modules/catalog/domain/keygen"
	"pimly.commerslab/backend-go/internal/modules/catalog/domain/variants"
	"pimly.commerslab/backend-go/internal/modules/channels/application/productimports"
	inventoryapp "pimly.commerslab/backend-go/internal/modules/inventory/application"
	mediaapp "pimly.commerslab/backend-go/internal/modules/media/application"
	pricingapp "pimly.commerslab/backend-go/internal/modules/pricing/application"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// maxImageBytes, indirilen tek görselin azami boyutudur (10 MB; .NET MaxImageBytes).
const maxImageBytes = 10 * 1024 * 1024

// imageDownloadConcurrency, aynı anda yürüyen görsel indirme sayısıdır
// (.NET sıralı indirir; Go'da sınırlı havuz kullanılır).
const imageDownloadConcurrency = 4

// imageRetryDelays, geçici indirme hatalarında denemeler arası beklemelerdir
// (Trendyol CDN'i ara sıra geçici DNS/timeout hatası verir; .NET ile aynı: 1s, 3s).
var imageRetryDelays = []time.Duration{time.Second, 3 * time.Second}

// Gateway, import hattının Catalog yazma kapısıdır;
// productimports.CatalogImportGateway portunu uygular.
type Gateway struct {
	pool *pgxpool.Pool

	categories *catalogapp.CategoryHandlers
	brands     *catalogapp.BrandHandlers
	attributes *catalogapp.AttributeHandlers
	variants   *catalogapp.VariantHandlers
	products   *catalogapp.ProductHandlers
	pricing    *pricingapp.PricingHandlers
	stock      *inventoryapp.StockHandlers
	upload     *mediaapp.UploadHandlers

	categoryRepo  catalogapp.CategoryRepository
	brandRepo     catalogapp.BrandRepository
	attributeRepo catalogapp.AttributeRepository
	variantRepo   catalogapp.VariantRepository
	productRepo   catalogapp.ProductRepository
	pricingRepo   pricingapp.PricingRepository

	httpClient *http.Client
}

// NewGateway, handler ve depolarla kapıyı oluşturur. httpClient nil ise 20
// saniyelik zaman aşımıyla varsayılan istemci kullanılır (görsel indirme).
func NewGateway(
	pool *pgxpool.Pool,
	categories *catalogapp.CategoryHandlers,
	brands *catalogapp.BrandHandlers,
	attributes *catalogapp.AttributeHandlers,
	variantHandlers *catalogapp.VariantHandlers,
	products *catalogapp.ProductHandlers,
	pricing *pricingapp.PricingHandlers,
	stock *inventoryapp.StockHandlers,
	upload *mediaapp.UploadHandlers,
	categoryRepo catalogapp.CategoryRepository,
	brandRepo catalogapp.BrandRepository,
	attributeRepo catalogapp.AttributeRepository,
	variantRepo catalogapp.VariantRepository,
	productRepo catalogapp.ProductRepository,
	pricingRepo pricingapp.PricingRepository,
	httpClient *http.Client,
) *Gateway {
	if httpClient == nil {
		httpClient = &http.Client{Timeout: 20 * time.Second}
	}
	return &Gateway{
		pool: pool, categories: categories, brands: brands, attributes: attributes,
		variants: variantHandlers, products: products, pricing: pricing,
		stock: stock, upload: upload,
		categoryRepo: categoryRepo, brandRepo: brandRepo, attributeRepo: attributeRepo,
		variantRepo: variantRepo, productRepo: productRepo, pricingRepo: pricingRepo,
		httpClient: httpClient,
	}
}

// EnsureCategoryPath, ad zincirini kategori ağacında garanti eder ve yaprağın
// kimliğini döner (.NET EnsureCategoryPathAsync portu).
func (g *Gateway) EnsureCategoryPath(ctx context.Context, tenantID uuid.UUID, pathSegments []string) sharedkernel.ResultOf[uuid.UUID] {
	if len(pathSegments) == 0 {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewValidationError("Category path is required."))
	}

	// Mevcut (üst, ad) → kimlik haritası tek sorguda kurulur; adlar duyarsız
	// karşılaştırılır (.NET ToLowerInvariant karşılığı).
	type parentNameKey struct {
		parentID uuid.UUID // kök için uuid.Nil
		name     string
	}
	byParentAndName := map[parentNameKey]uuid.UUID{}
	rows, err := g.pool.Query(ctx,
		`SELECT id, COALESCE(parent_id, '00000000-0000-0000-0000-000000000000'::uuid), name
		 FROM catalog.categories WHERE tenant_id = $1`, tenantID)
	if err != nil {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewInternalError(err.Error()))
	}
	for rows.Next() {
		var id, parentID uuid.UUID
		var name string
		if err := rows.Scan(&id, &parentID, &name); err != nil {
			rows.Close()
			return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewInternalError(err.Error()))
		}
		key := parentNameKey{parentID: parentID, name: strings.ToLower(strings.TrimSpace(name))}
		if _, exists := byParentAndName[key]; !exists {
			byParentAndName[key] = id
		}
	}
	rows.Close()
	if err := rows.Err(); err != nil {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewInternalError(err.Error()))
	}

	parentID := uuid.Nil
	for _, segment := range pathSegments {
		trimmed := strings.TrimSpace(segment)
		key := parentNameKey{parentID: parentID, name: strings.ToLower(trimmed)}
		if categoryID, exists := byParentAndName[key]; exists {
			parentID = categoryID
			continue
		}
		var parentPtr *uuid.UUID
		if parentID != uuid.Nil {
			value := parentID
			parentPtr = &value
		}
		createResult := g.categories.Create(ctx, tenantID, catalogapp.CreateCategoryCommand{
			Name: trimmed, Code: nil, ParentID: parentPtr})
		if createResult.IsFailure() {
			return sharedkernel.FailOf[uuid.UUID](createResult.Err())
		}
		parentID = createResult.Value().ID
		byParentAndName[key] = parentID
	}
	return sharedkernel.OkOf(parentID)
}

// CategoryExists, kategorinin var olup olmadığını döner.
func (g *Gateway) CategoryExists(ctx context.Context, tenantID, categoryID uuid.UUID) (bool, error) {
	category, err := g.categoryRepo.GetByID(ctx, tenantID, categoryID)
	if err != nil {
		return false, err
	}
	return category != nil, nil
}

// EnsureBrand, markayı ada göre garanti eder ve kimliğini döner
// (.NET EnsureBrandAsync portu; externalID marka koduna yazılır).
func (g *Gateway) EnsureBrand(ctx context.Context, tenantID uuid.UUID, name string, externalID *string) sharedkernel.ResultOf[uuid.UUID] {
	if strings.TrimSpace(name) == "" {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewValidationError("Brand name is required."))
	}
	trimmed := strings.TrimSpace(name)
	existing, err := g.brandRepo.GetByName(ctx, tenantID, trimmed)
	if err != nil {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewInternalError(err.Error()))
	}
	if existing != nil {
		return sharedkernel.OkOf(existing.ID)
	}
	createResult := g.brands.Create(ctx, tenantID, catalogapp.CreateBrandCommand{Name: trimmed, Code: externalID})
	if createResult.IsFailure() {
		return sharedkernel.FailOf[uuid.UUID](createResult.Err())
	}
	return sharedkernel.OkOf(createResult.Value().ID)
}

// EnsureAttribute, özelliği ada göre (duyarsız) garanti eder
// (.NET EnsureAttributeAsync portu).
func (g *Gateway) EnsureAttribute(ctx context.Context, tenantID uuid.UUID, name string) sharedkernel.ResultOf[uuid.UUID] {
	trimmed := strings.TrimSpace(name)

	// Ad eşleşmesi tüm tanımlar üzerinden duyarsız yapılır (.NET ListAsync +
	// OrdinalIgnoreCase); tenant başına tanım sayısı küçüktür.
	rows, err := g.pool.Query(ctx,
		`SELECT id, name FROM catalog.attributes WHERE tenant_id = $1`, tenantID)
	if err != nil {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewInternalError(err.Error()))
	}
	for rows.Next() {
		var id uuid.UUID
		var existingName string
		if err := rows.Scan(&id, &existingName); err != nil {
			rows.Close()
			return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewInternalError(err.Error()))
		}
		if strings.EqualFold(existingName, trimmed) {
			rows.Close()
			return sharedkernel.OkOf(id)
		}
	}
	rows.Close()
	if err := rows.Err(); err != nil {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewInternalError(err.Error()))
	}

	createResult := g.attributes.Create(ctx, tenantID, catalogapp.CreateAttributeCommand{Name: trimmed})
	if createResult.IsFailure() {
		return sharedkernel.FailOf[uuid.UUID](createResult.Err())
	}
	return sharedkernel.OkOf(createResult.Value().ID)
}

// EnsureAttributeValue, özellik değerini ada göre (duyarsız) garanti eder
// (.NET EnsureAttributeValueAsync portu).
func (g *Gateway) EnsureAttributeValue(ctx context.Context, tenantID, attributeID uuid.UUID, valueName string) sharedkernel.ResultOf[uuid.UUID] {
	attribute, err := g.attributeRepo.GetByID(ctx, tenantID, attributeID)
	if err != nil {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewInternalError(err.Error()))
	}
	if attribute == nil {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewNotFoundError("Attribute not found."))
	}
	trimmed := strings.TrimSpace(valueName)
	for _, value := range attribute.Values {
		if strings.EqualFold(value.Name, trimmed) {
			return sharedkernel.OkOf(value.ID)
		}
	}
	addResult := g.attributes.AddValue(ctx, tenantID, catalogapp.AddAttributeValueCommand{
		AttributeID: attributeID, Name: trimmed})
	if addResult.IsFailure() {
		return sharedkernel.FailOf[uuid.UUID](addResult.Err())
	}
	return sharedkernel.OkOf(addResult.Value().ID)
}

// EnsureVariant, varyant eksenini ada göre garanti eder; tenant başına tek
// slicer ekseni kuralını uygular: başka bir slicer varsa eksen slicer'sız
// açılır (.NET EnsureVariantAsync portu).
func (g *Gateway) EnsureVariant(ctx context.Context, tenantID uuid.UUID, name string, isColor, slicer bool) sharedkernel.ResultOf[productimports.EnsuredVariantSnapshot] {
	trimmed := strings.TrimSpace(name)
	existing, err := g.variantRepo.GetByName(ctx, tenantID, trimmed)
	if err != nil {
		return sharedkernel.FailOf[productimports.EnsuredVariantSnapshot](sharedkernel.NewInternalError(err.Error()))
	}
	if existing != nil {
		return sharedkernel.OkOf(productimports.EnsuredVariantSnapshot{
			ID: existing.ID, Name: existing.Name,
			IsColor: existing.SelectionStyle == variants.StyleColor,
			Slicer:  existing.Slicer, SlicerDemoted: slicer && !existing.Slicer,
		})
	}

	slicerDemoted := false
	if slicer {
		currentSlicer, err := g.variantRepo.GetSlicerVariant(ctx, tenantID, nil)
		if err != nil {
			return sharedkernel.FailOf[productimports.EnsuredVariantSnapshot](sharedkernel.NewInternalError(err.Error()))
		}
		if currentSlicer != nil {
			slicer = false
			slicerDemoted = true
		}
	}
	style := "list"
	if isColor {
		style = "color"
	}
	createResult := g.variants.Create(ctx, tenantID, catalogapp.CreateVariantTypeCommand{
		Name: trimmed, SelectionStyle: style, SortOrder: 0, Slicer: slicer})
	if createResult.IsFailure() {
		return sharedkernel.FailOf[productimports.EnsuredVariantSnapshot](createResult.Err())
	}
	created := createResult.Value()
	return sharedkernel.OkOf(productimports.EnsuredVariantSnapshot{
		ID: created.ID, Name: created.Name, IsColor: isColor,
		Slicer: created.Slicer, SlicerDemoted: slicerDemoted,
	})
}

// EnsureVariantValue, eksen değerini garanti eder. Etiket eşleşmesi VEYA aynı
// slug-anahtara indirgenen mevcut değer → yeniden kullanılır: Trendyol'da
// yalnızca boşluk/noktalama ile ayrışan değerler (ör. "80x200" ↔ "80 x 200")
// aynı anahtarı üretir; bunları ayrı değer olarak eklemeye çalışmak anahtar
// çakışması ("Variant value key must be unique") verip ürünü hataya sokardı
// (.NET EnsureVariantValueAsync portu).
func (g *Gateway) EnsureVariantValue(ctx context.Context, tenantID, variantID uuid.UUID, label string) sharedkernel.ResultOf[uuid.UUID] {
	variant, err := g.variantRepo.GetByID(ctx, tenantID, variantID)
	if err != nil {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewInternalError(err.Error()))
	}
	if variant == nil {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewNotFoundError("Variant type not found."))
	}
	trimmed := strings.TrimSpace(label)
	previewKey := ""
	if keyResult := keygen.FromName(trimmed); keyResult.IsSuccess() {
		previewKey = keyResult.Value()
	}
	for _, value := range variant.Values {
		if strings.EqualFold(value.Label, trimmed) ||
			(previewKey != "" && strings.EqualFold(value.Key, previewKey)) {
			return sharedkernel.OkOf(value.ID)
		}
	}
	addResult := g.variants.AddValue(ctx, tenantID, catalogapp.VariantValueCommand{
		VariantTypeID: variantID, Label: trimmed, SortOrder: 0})
	if addResult.IsFailure() {
		return sharedkernel.FailOf[uuid.UUID](addResult.Err())
	}
	return sharedkernel.OkOf(addResult.Value().ID)
}

// AssignAttributeToCategory, özelliği kategoriye atar (.NET
// AssignAttributeToCategoryAsync portu). Seviye yalnızca yükseltilir
// (model → slicer/kalem); kullanıcının elle verdiği daha özgül seviye import
// tarafından asla model'e düşürülmez. Eşzamanlı atama çakışması idempotentlik
// açısından başarı sayılır.
func (g *Gateway) AssignAttributeToCategory(ctx context.Context, tenantID, categoryID, attributeID uuid.UUID, required bool, sortOrder int, scope productimports.PlannedAttributeScope) sharedkernel.Result {
	category, err := g.categoryRepo.GetByID(ctx, tenantID, categoryID)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if category == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Category not found."))
	}
	mappedScope := mapScope(scope)
	for _, assignment := range category.Assignments {
		if assignment.AttributeID != attributeID {
			continue
		}
		if assignment.Scope == categories.ScopeModel && mappedScope != categories.ScopeModel {
			scopeCopy := mappedScope
			upgradeResult := g.categories.UpdateAssignment(ctx, tenantID, catalogapp.UpdateCategoryAttributeCommand{
				ID: assignment.ID, Required: assignment.Required,
				SortOrder: assignment.SortOrder, Scope: &scopeCopy})
			if upgradeResult.IsFailure() {
				return sharedkernel.Fail(upgradeResult.Err())
			}
		}
		return sharedkernel.Ok()
	}
	assignResult := g.categories.AssignAttribute(ctx, tenantID, catalogapp.AssignCategoryAttributeCommand{
		CategoryID: categoryID, AttributeID: attributeID,
		Required: required, SortOrder: sortOrder, Scope: mappedScope})
	if assignResult.IsFailure() {
		if assignResult.Err().Code == sharedkernel.ErrorCodeConflict {
			return sharedkernel.Ok()
		}
		return sharedkernel.Fail(assignResult.Err())
	}
	return sharedkernel.Ok()
}

// ProductGroupExists, model kodu ya da barkodlardan biri katalogda varsa true
// döner (.NET ProductGroupExistsAsync portu).
func (g *Gateway) ProductGroupExists(ctx context.Context, tenantID uuid.UUID, modelCode string, barcodes []string) (bool, error) {
	exists, err := g.productRepo.ModelCodeExists(ctx, tenantID, modelCode)
	if err != nil || exists {
		return exists, err
	}
	for _, barcode := range barcodes {
		exists, err := g.productRepo.BarcodeExists(ctx, tenantID, barcode)
		if err != nil || exists {
			return exists, err
		}
	}
	return false, nil
}

// ResolveItemIDsByBarcode, barkodları mevcut kalem kimliklerine çözer
// (.NET ResolveItemIdsByBarcodeAsync portu).
func (g *Gateway) ResolveItemIDsByBarcode(ctx context.Context, tenantID uuid.UUID, barcodes []string) (map[string]uuid.UUID, error) {
	result := map[string]uuid.UUID{}
	if len(barcodes) == 0 {
		return result, nil
	}
	rows, err := g.pool.Query(ctx,
		`SELECT barcode, id FROM catalog.product_items
		 WHERE tenant_id = $1 AND barcode = ANY($2)`, tenantID, barcodes)
	if err != nil {
		return nil, fmt.Errorf("catalogimport: kalem kimlikleri çözülemedi: %w", err)
	}
	defer rows.Close()
	for rows.Next() {
		var barcode string
		var id uuid.UUID
		if err := rows.Scan(&barcode, &id); err != nil {
			return nil, err
		}
		result[barcode] = id
	}
	return result, rows.Err()
}

// CreateProductsBatch, ürün grubunu toplu oluşturur; kalem başına temel fiyat
// ve stok da yazılır (.NET CreateProductsBatchAsync portu). Import edilen veri
// kaynağın gerçeğidir; PIM'de sonradan eklenen zorunlu özellikler importu
// bloklamaz (EnforceRequiredAttributes=false) — hazırlık kontrolüne bırakılır.
func (g *Gateway) CreateProductsBatch(ctx context.Context, tenantID uuid.UUID, input productimports.CatalogProductBatchInput) sharedkernel.ResultOf[[]productimports.CreatedProductSnapshot] {
	attributeInputs := make([]catalogapp.AttributeValueInput, len(input.AttributeValues))
	for i, selection := range input.AttributeValues {
		attributeInputs[i] = catalogapp.AttributeValueInput{
			AttributeID: selection.ID, AttributeValueID: selection.ValueID}
	}
	variantInputs := make([]catalogapp.VariantRefInput, len(input.Variants))
	for i, axis := range input.Variants {
		variantInputs[i] = catalogapp.VariantRefInput{ID: axis.VariantID}
	}
	itemInputs := make([]catalogapp.CreateProductItemInput, len(input.Items))
	for i, item := range input.Items {
		itemAttributes := make([]catalogapp.AttributeValueInput, len(item.AttributeValues))
		for j, selection := range item.AttributeValues {
			itemAttributes[j] = catalogapp.AttributeValueInput{
				AttributeID: selection.ID, AttributeValueID: selection.ValueID}
		}
		itemVariants := make([]catalogapp.VariantValueInput, len(item.VariantValues))
		for j, selection := range item.VariantValues {
			itemVariants[j] = catalogapp.VariantValueInput{
				VariantID: selection.ID, VariantValueID: selection.ValueID}
		}
		itemInputs[i] = catalogapp.CreateProductItemInput{
			Sku: item.Sku, Barcode: item.Barcode,
			AttributeValues: itemAttributes, VariantValues: itemVariants,
		}
	}
	splitInputs := make([]catalogapp.BatchSplitInput, len(input.Splits))
	for i, split := range input.Splits {
		var splitAttributes []catalogapp.AttributeValueInput
		for _, selection := range split.AttributeValues {
			splitAttributes = append(splitAttributes, catalogapp.AttributeValueInput{
				AttributeID: selection.ID, AttributeValueID: selection.ValueID})
		}
		splitInputs[i] = catalogapp.BatchSplitInput{
			ValueName: split.ValueName, ModelCode: split.ModelCode,
			Name: split.Name, Description: split.Description,
			AttributeValues: splitAttributes,
		}
	}

	createResult := g.products.CreateBatch(ctx, tenantID, catalogapp.CreateProductsBatchCommand{
		GroupID: input.GroupID,
		Products: []catalogapp.CreateProductsBatchItem{{
			CategoryID: input.CategoryID, ModelCode: input.ModelCode,
			Name: input.Name, Status: input.Status,
			Attributes: attributeInputs, Variants: variantInputs,
			Items: itemInputs, Splits: splitInputs,
			BrandID: input.BrandID, Description: input.Description,
		}},
		EnforceRequiredAttributes: false,
	})
	if createResult.IsFailure() {
		return sharedkernel.FailOf[[]productimports.CreatedProductSnapshot](createResult.Err())
	}

	snapshots := make([]productimports.CreatedProductSnapshot, 0, len(createResult.Value().Products))
	itemIDByBarcode := map[string]uuid.UUID{}
	for _, product := range createResult.Value().Products {
		snapshot := productimports.CreatedProductSnapshot{
			ProductID: product.ID, ItemIDByBarcode: map[string]uuid.UUID{}}
		for _, item := range product.Items {
			snapshot.ItemIDByBarcode[item.Barcode] = item.ID
			itemIDByBarcode[strings.ToLower(item.Barcode)] = item.ID
		}
		snapshots = append(snapshots, snapshot)
	}

	// Temel fiyat ve stok, kalem Catalog'da oluşturulduktan sonra Pricing ve
	// Inventory'ye yazılır (dual-write; Catalog kolonları contract dilimine
	// kadar dormant kalır).
	for _, item := range input.Items {
		itemID, ok := itemIDByBarcode[strings.ToLower(item.Barcode)]
		if !ok {
			continue
		}
		var compareAt *pricingapp.Decimal
		if item.CompareAtPrice != nil {
			value := pricingapp.Decimal(*item.CompareAtPrice)
			compareAt = &value
		}
		// .NET import yolu base price'a currency geçmez (SetBasePriceCommand
		// Currency=null); para birimi varsayılan davranışa bırakılır.
		basePriceResult := g.pricing.SetBasePrice(ctx, tenantID, itemID,
			pricingapp.Decimal(item.Price), compareAt, nil)
		if basePriceResult.IsFailure() {
			return sharedkernel.FailOf[[]productimports.CreatedProductSnapshot](basePriceResult.Err())
		}
		stockResult := g.stock.Set(ctx, tenantID, itemID, item.Stock)
		if stockResult.IsFailure() {
			return sharedkernel.FailOf[[]productimports.CreatedProductSnapshot](stockResult.Err())
		}
	}
	return sharedkernel.OkOf(snapshots)
}

// EnsurePriceDefinition, fiyat tanımını ada göre garanti eder
// (.NET EnsurePriceDefinitionAsync portu).
func (g *Gateway) EnsurePriceDefinition(ctx context.Context, tenantID uuid.UUID, name string, code *string) sharedkernel.ResultOf[uuid.UUID] {
	if strings.TrimSpace(name) == "" {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewValidationError("Price definition name is required."))
	}
	trimmed := strings.TrimSpace(name)
	existing, err := g.pricingRepo.GetDefinitionByName(ctx, tenantID, trimmed)
	if err != nil {
		return sharedkernel.FailOf[uuid.UUID](sharedkernel.NewInternalError(err.Error()))
	}
	if existing != nil {
		return sharedkernel.OkOf(existing.ID)
	}
	createResult := g.pricing.CreateDefinition(ctx, tenantID, trimmed, code)
	if createResult.IsFailure() {
		return sharedkernel.FailOf[uuid.UUID](createResult.Err())
	}
	return sharedkernel.OkOf(createResult.Value().ID)
}

// UpsertItemPrice, kalemin tanım bazlı fiyatını yazar/günceller
// (.NET UpsertItemPriceAsync portu).
func (g *Gateway) UpsertItemPrice(ctx context.Context, tenantID, productItemID, priceDefinitionID uuid.UUID, amount string, currency *string) sharedkernel.Result {
	upsertResult := g.pricing.UpsertItemPrice(ctx, tenantID, productItemID, priceDefinitionID,
		pricingapp.Decimal(amount), currency)
	if upsertResult.IsFailure() {
		return sharedkernel.Fail(upsertResult.Err())
	}
	return sharedkernel.Ok()
}

// downloadedImage, indirme havuzunun tek sonucu.
type downloadedImage struct {
	content []byte
	err     *sharedkernel.Error
}

// AddProductImages, harici görselleri medya deposuna alıp ürüne sırayla bağlar
// (.NET AddProductImageAsync döngüsünün toplu karşılığı). İndirme en fazla 4
// paralel yürür; yükleme ve bağlama sırayı korur (ilk görsel birincil). İlk
// hatada durur ve hatayı döner — o ana kadar bağlanan görseller kalır.
func (g *Gateway) AddProductImages(ctx context.Context, tenantID, productID uuid.UUID, sourceURLs []string) sharedkernel.Result {
	if len(sourceURLs) == 0 {
		return sharedkernel.Ok()
	}

	// Sınırlı eşzamanlılıkla indirme: sonuçlar kaynak sırasına yazılır.
	results := make([]downloadedImage, len(sourceURLs))
	semaphore := make(chan struct{}, imageDownloadConcurrency)
	var wg sync.WaitGroup
	for i, sourceURL := range sourceURLs {
		wg.Add(1)
		go func(index int, target string) {
			defer wg.Done()
			semaphore <- struct{}{}
			defer func() { <-semaphore }()
			content, err := g.downloadImageWithRetry(ctx, target)
			results[index] = downloadedImage{content: content, err: err}
		}(i, sourceURL)
	}
	wg.Wait()

	for sortOrder, result := range results {
		if result.err != nil {
			return sharedkernel.Fail(result.err)
		}
		uploadResult := g.upload.Upload(ctx, tenantID, result.content, mediaapp.PurposeProduct)
		if uploadResult.IsFailure() {
			return sharedkernel.Fail(uploadResult.Err())
		}
		addResult := g.products.AddImage(ctx, tenantID, catalogapp.ProductImageCommand{
			ProductID: productID, URL: uploadResult.Value().URL,
			SortOrder: sortOrder, IsPrimary: sortOrder == 0,
		})
		if addResult.IsFailure() {
			return sharedkernel.Fail(addResult.Err())
		}
	}
	return sharedkernel.Ok()
}

// downloadImageWithRetry, görseli artan beklemeyle en fazla 3 kez dener
// (.NET DownloadImageWithRetryAsync portu: 1s, 3s). Kalıcı hata ve gerçek
// iptal aynen üst katmana taşınır; 10 MB üzeri görsel reddedilir.
func (g *Gateway) downloadImageWithRetry(ctx context.Context, sourceURL string) ([]byte, *sharedkernel.Error) {
	for attempt := 0; ; attempt++ {
		content, transient, err := g.downloadImageOnce(ctx, sourceURL)
		if err == nil {
			return content, nil
		}
		if transient && ctx.Err() == nil && attempt < len(imageRetryDelays) {
			select {
			case <-time.After(imageRetryDelays[attempt]):
				continue
			case <-ctx.Done():
				return nil, sharedkernel.NewFailureError("Image download cancelled: " + ctx.Err().Error())
			}
		}
		return nil, err
	}
}

// downloadImageOnce, tek indirme denemesidir; transient, hatanın yeniden
// denenebilir olup olmadığını belirtir.
func (g *Gateway) downloadImageOnce(ctx context.Context, sourceURL string) (content []byte, transient bool, failure *sharedkernel.Error) {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, sourceURL, nil)
	if err != nil {
		return nil, false, sharedkernel.NewFailureError("Image download failed: " + err.Error())
	}
	resp, err := g.httpClient.Do(req)
	if err != nil {
		// Ağ/zaman aşımı hataları geçicidir (.NET HttpRequestException /
		// TaskCanceledException karşılığı); gerçek iptal geçici sayılmaz.
		if errors.Is(err, context.Canceled) {
			return nil, false, sharedkernel.NewFailureError("Image download failed: " + err.Error())
		}
		return nil, true, sharedkernel.NewFailureError("Image download failed: " + err.Error())
	}
	defer resp.Body.Close()

	if resp.StatusCode < 200 || resp.StatusCode >= 300 {
		return nil, false, sharedkernel.NewFailureError(fmt.Sprintf(
			"Image download failed with status %d.", resp.StatusCode))
	}
	if resp.ContentLength > maxImageBytes {
		return nil, false, sharedkernel.NewValidationError("Image exceeds the allowed size.")
	}
	var buffer bytes.Buffer
	if _, err := io.Copy(&buffer, io.LimitReader(resp.Body, maxImageBytes+1)); err != nil {
		return nil, true, sharedkernel.NewFailureError("Image download failed: " + err.Error())
	}
	if buffer.Len() > maxImageBytes {
		return nil, false, sharedkernel.NewValidationError("Image exceeds the allowed size.")
	}
	return buffer.Bytes(), false, nil
}

// mapScope, planlayıcı seviyesini catalog seviye tipine çevirir
// (.NET MapScope portu).
func mapScope(scope productimports.PlannedAttributeScope) categories.AttributeScope {
	switch scope {
	case productimports.ScopeSlicer:
		return categories.ScopeSlicer
	case productimports.ScopeItem:
		return categories.ScopeItem
	default:
		return categories.ScopeModel
	}
}
