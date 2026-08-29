// Package productimports, pazaryerinden ürün içe aktarma hattının uygulama
// katmanıdır (.NET Channels.Application/ProductImports karşılığı). İki ana
// parçadan oluşur:
//
//   - Planlayıcı (planner.go): pazaryeri ürün satırlarını Pimly ürün gruplarına
//     dönüştüren SAF fonksiyon — depo/IO bağımlılığı yoktur, birebir test edilir.
//   - İşlemci (process.go): claim edilmiş import işini uçtan uca yürüten
//     orkestratör (.NET ProcessProductImportHandler portu).
//
// Tutarlar ham ondalık dizgi olarak taşınır (449.90 hiçbir katmanda 449.9'a
// çökmez); karşılaştırmalar big.Rat ile kayıpsız yapılır.
package productimports

import (
	"context"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// --- Pazaryeri ürün sayfası sözleşmesi (.NET IMarketplaceProductsClient) ---

// MarketplaceProductAttributeNode, pazaryeri ürün satırındaki tek özellik
// değeridir (.NET MarketplaceProductAttributeNode karşılığı).
type MarketplaceProductAttributeNode struct {
	// ExternalAttributeID, pazaryerindeki özellik kimliğidir (dizgi).
	ExternalAttributeID string

	// Name, pazaryerinin bildirdiği özellik adıdır.
	Name string

	// ExternalValueID, seçili değerin pazaryeri kimliğidir; serbest metin
	// değerlerde nil olur.
	ExternalValueID *string

	// Value, pazaryeri sözlüğünden gelen değer adıdır; yoksa nil.
	Value *string

	// CustomValue, satıcının girdiği serbest metin değeridir; yoksa nil.
	CustomValue *string
}

// MarketplaceProductNode, pazaryerinden çekilen tek ürün satırıdır
// (.NET MarketplaceProductNode karşılığı). Trendyol'da her satır bir barkod
// (satılabilir kalem) temsil eder; varyantlar ProductMainID ile gruplanır.
type MarketplaceProductNode struct {
	// Barcode, kalemin pazaryeri listeleme kimliğidir (boş satırlar istemcide elenir).
	Barcode string

	// Title, ürünün pazaryerindeki listeleme başlığıdır.
	Title string

	// ProductMainID, varyant grubunun ortak kimliğidir; pazaryerinde boşsa
	// istemci barkodu kullanır.
	ProductMainID string

	// Brand, marka adıdır; yoksa nil.
	Brand *string

	// StockCode, satıcının kendi stok kodudur (SKU adayı); yoksa nil.
	StockCode *string

	// Quantity, pazaryerindeki satılabilir stok adedidir.
	Quantity int

	// ListPrice, üstü çizili (liste) fiyatın ham ondalık dizgisidir.
	ListPrice string

	// SalePrice, satış fiyatının ham ondalık dizgisidir.
	SalePrice string

	// CurrencyType, para birimi kodudur (ör. TRY); yoksa nil.
	CurrencyType *string

	// ExternalCategoryID, ürünün pazaryeri kategori kimliğidir; boş olabilir.
	ExternalCategoryID string

	// CategoryName, pazaryeri kategori adıdır; yoksa nil.
	CategoryName *string

	// Description, ürün açıklaması (HTML olabilir); yoksa nil.
	Description *string

	// Approved, satırın pazaryerinde onaylı olup olmadığıdır.
	Approved bool

	// ImageURLs, satırın görsel bağlantılarıdır (sıralı).
	ImageURLs []string

	// Attributes, satırın özellik değerleridir (düz liste).
	Attributes []MarketplaceProductAttributeNode

	// BrandExternalID, pazaryeri marka kimliğidir; yoksa nil.
	BrandExternalID *string
}

// MarketplaceProductPage, sayfalı ürün listesi yanıtıdır
// (.NET MarketplaceProductPage karşılığı).
type MarketplaceProductPage struct {
	// TotalElements, pazaryerindeki toplam satır sayısıdır.
	TotalElements int64

	// TotalPages, toplam sayfa sayısıdır.
	TotalPages int

	// Page, bu yanıtın sıfır tabanlı sayfa numarasıdır.
	Page int

	// Size, sayfa boyutudur.
	Size int

	// Items, bu sayfanın ürün satırlarıdır.
	Items []MarketplaceProductNode
}

// MarketplaceProductsClient, pazaryerinden sayfalı ürün listesini çeken porttur
// (.NET IMarketplaceProductsClient karşılığı).
type MarketplaceProductsClient interface {
	// FetchProductsPage, verilen sayfayı çeker; kimlik bilgisi eksikse
	// doğrulama hatası döner.
	FetchProductsPage(ctx context.Context, credentials *application.MarketplaceCredentials, page, size int) sharedkernel.ResultOf[MarketplaceProductPage]
}

// --- Planlayıcı sözleşmesi (.NET Planning kayıtları) ---

// ProductImportAttributeDef, kategori özellik tanımının planlayıcıya taşınan
// projeksiyonudur (cache'ten; .NET ProductImportAttributeDef karşılığı).
type ProductImportAttributeDef struct {
	// ExternalAttributeID, pazaryerindeki özellik kimliğidir.
	ExternalAttributeID string

	// Name, özelliğin pazaryerindeki adıdır.
	Name string

	// Required, özelliğin kategoride zorunlu olup olmadığıdır.
	Required bool

	// AllowCustom, serbest metin değere izin verilip verilmediğidir.
	AllowCustom bool

	// IsVariant, özelliğin pazaryerinde varyant ekseni olduğudur.
	IsVariant bool

	// IsSlicer, özelliğin bölme (renk) ekseni olduğudur.
	IsSlicer bool
}

// ProductImportPlan, planlayıcı çıktısıdır: grup başına bir plan.
type ProductImportPlan struct {
	// Groups, ProductMainID sırasına göre dizilmiş grup planlarıdır.
	Groups []ProductGroupPlan
}

// PlannedAttributeScope, içe aktarılan özelliğin tespit edilen seviyesidir;
// kategori atamasına da yazılır (.NET PlannedAttributeScope karşılığı).
type PlannedAttributeScope int

// Özellik seviyeleri.
const (
	// ScopeModel: model (ürün) başına tek değer.
	ScopeModel PlannedAttributeScope = 0

	// ScopeSlicer: slicer (renk) değeri başına değer; bölünen ürüne yazılır.
	ScopeSlicer PlannedAttributeScope = 1

	// ScopeItem: satılabilir kalem başına değer.
	ScopeItem PlannedAttributeScope = 2
)

// PlannedVariantAxis, planlanan varyant eksenidir.
type PlannedVariantAxis struct {
	// ExternalAttributeID, eksenin pazaryeri özellik kimliğidir.
	ExternalAttributeID string

	// Name, eksenin görünen adıdır (ör. Renk, Beden).
	Name string

	// IsColor, eksenin renk seçim stiliyle açılacağını belirtir.
	IsColor bool

	// Slicer, eksenin bölme (slicer) ekseni olduğunu belirtir.
	Slicer bool
}

// PlannedAttributeValue, planlanan özellik değeridir; Scope hangi seviyede
// yazılacağını belirtir.
type PlannedAttributeValue struct {
	// ExternalAttributeID, özelliğin pazaryeri kimliğidir.
	ExternalAttributeID string

	// AttributeName, özelliğin adıdır (öncelik kategori tanımındaki ad).
	AttributeName string

	// ValueName, seçili değerin adıdır.
	ValueName string

	// ExternalValueID, değerin pazaryeri kimliğidir; serbest metinde nil.
	ExternalValueID *string

	// Required, özelliğin kategori tanımında zorunlu olduğudur.
	Required bool

	// Scope, değerin yazılacağı seviyedir.
	Scope PlannedAttributeScope
}

// PlannedVariantSelection, kalemin bir eksendeki seçimidir.
type PlannedVariantSelection struct {
	// ExternalAttributeID, eksenin pazaryeri kimliğidir.
	ExternalAttributeID string

	// ValueName, seçilen değer adıdır.
	ValueName string

	// ExternalValueID, değerin pazaryeri kimliğidir; serbest metinde nil.
	ExternalValueID *string
}

// PlannedItem, planlanan satılabilir kalemdir.
type PlannedItem struct {
	// Barcode, kalemin barkodudur.
	Barcode string

	// Sku, türetilen stok kodudur; tekilleştirilemezse nil (çakışan SKU yazılmaz).
	Sku *string

	// Price, satış fiyatının ham ondalık dizgisidir.
	Price string

	// CompareAtPrice, üstü çizili fiyattır; yalnızca ListPrice > SalePrice ise dolu.
	CompareAtPrice *string

	// Stock, negatif olmayan stok adedidir.
	Stock int

	// Currency, para birimi kodudur; yoksa nil.
	Currency *string

	// VariantSelections, kalemin eksen seçimleridir.
	VariantSelections []PlannedVariantSelection

	// ItemAttributeValues, kalem düzeyine yazılacak özellik değerleridir.
	ItemAttributeValues []PlannedAttributeValue

	// ImageURLs, kalemin satırındaki görsel bağlantılarıdır.
	ImageURLs []string
}

// PlannedSplit, slicer değerine özel plan geçersiz kılmasıdır: gerçek stok
// kodu, orijinal başlık/açıklama ve renk-bazlı özellik değerleri
// (ör. ValueName "Antrasit", StockCode "25CSM02817GR52").
type PlannedSplit struct {
	// ValueName, slicer (renk) değerinin adıdır.
	ValueName string

	// StockCode, bu renge özgü gerçek stok kodudur; güvenilir değilse nil.
	StockCode *string

	// Title, bu rengin orijinal listeleme başlığıdır; yoksa nil.
	Title *string

	// Description, bu rengin orijinal açıklamasıdır; yoksa nil.
	Description *string

	// SplitAttributeValues, bu slicer değerinin ürününe yazılacak özellik
	// değerleridir; boş olabilir.
	SplitAttributeValues []PlannedAttributeValue
}

// ProductGroupPlan, tek ürün grubunun (ProductMainID) import planıdır.
type ProductGroupPlan struct {
	// ProductMainID, grubun pazaryeri kimliğidir.
	ProductMainID string

	// Name, grubun (ilk satırın) başlığıdır.
	Name string

	// ExternalCategoryID, grubun pazaryeri kategori kimliğidir.
	ExternalCategoryID string

	// ModelCode, Pimly ürününün model kodudur (= ProductMainID).
	ModelCode string

	// VariantAxes, planlanan varyant eksenleridir (en fazla 3).
	VariantAxes []PlannedVariantAxis

	// AttributeValues, model düzeyi özellik değerleridir.
	AttributeValues []PlannedAttributeValue

	// Items, planlanan satılabilir kalemlerdir.
	Items []PlannedItem

	// Warnings, plan kurulurken üretilen uyarılardır (run hatalarına yazılır).
	Warnings []string

	// Error, grup plansız kaldıysa nedenidir; nil değilse grup atlanır.
	Error *string

	// SplitOverrides, slicer değeri başına kod/başlık geçersiz kılmalarıdır.
	SplitOverrides []PlannedSplit

	// BrandName, grubun marka adıdır; yoksa nil.
	BrandName *string

	// BrandExternalID, pazaryeri marka kimliğidir; yoksa nil.
	BrandExternalID *string

	// Description, grubun (ilk satırın) açıklamasıdır; yoksa nil.
	Description *string
}

// failedGroupPlan, grubu hata ile işaretleyen kısayoldur (.NET ProductGroupPlan.Failed).
func failedGroupPlan(productMainID, name, errorMessage string) ProductGroupPlan {
	return ProductGroupPlan{
		ProductMainID: productMainID, Name: name, ModelCode: productMainID,
		Error: &errorMessage,
	}
}

// --- Catalog yazma kapısı sözleşmesi (.NET ICatalogImportGateway) ---

// CatalogSelectionInput, (tanım kimliği, değer kimliği) seçim çiftidir.
type CatalogSelectionInput struct {
	// ID, özellik ya da varyant tanımının catalog kimliğidir.
	ID uuid.UUID

	// ValueID, seçilen değerin catalog kimliğidir.
	ValueID uuid.UUID
}

// CatalogVariantAxisInput, toplu oluşturma girdisindeki eksen tanımıdır.
type CatalogVariantAxisInput struct {
	// VariantID, eksenin catalog kimliğidir.
	VariantID uuid.UUID

	// IsColor, eksenin renk stiliyle açıldığıdır.
	IsColor bool

	// Slicer, eksenin slicer olduğudur.
	Slicer bool
}

// CatalogProductItemInput, toplu oluşturma girdisindeki kalemdir.
type CatalogProductItemInput struct {
	// Sku, kalemin stok kodudur; yoksa nil.
	Sku *string

	// Barcode, kalemin barkodudur.
	Barcode string

	// Price, satış fiyatının ham ondalık dizgisidir (temel fiyata yazılır).
	Price string

	// CompareAtPrice, üstü çizili fiyattır; yoksa nil.
	CompareAtPrice *string

	// Stock, başlangıç stok adedidir.
	Stock int

	// Currency, para birimi kodudur; yoksa nil.
	Currency *string

	// VariantValues, kalemin eksen seçimleridir.
	VariantValues []CatalogSelectionInput

	// AttributeValues, kalem düzeyi özellik seçimleridir.
	AttributeValues []CatalogSelectionInput
}

// CatalogSplitInput, slicer değeri başına geçersiz kılmalardır.
type CatalogSplitInput struct {
	// ValueName, slicer değerinin adıdır.
	ValueName string

	// ModelCode, bu bölmeye özgü model kodudur (renk stok kodu); yoksa nil.
	ModelCode *string

	// Name, bu bölmenin başlığıdır; yoksa nil.
	Name *string

	// Description, bu bölmenin açıklamasıdır; yoksa nil.
	Description *string

	// AttributeValues, bu bölmenin ürününe yazılacak seçimlerdir; boş olabilir.
	AttributeValues []CatalogSelectionInput
}

// CatalogProductBatchInput, toplu ürün oluşturma girdisidir
// (.NET CatalogProductBatchInput karşılığı).
type CatalogProductBatchInput struct {
	// GroupID, oluşturulacak ürün grubunun kimliğidir.
	GroupID uuid.UUID

	// CategoryID, hedef catalog kategorisidir.
	CategoryID uuid.UUID

	// ModelCode, ürünün model kodudur.
	ModelCode string

	// Name, ürünün adıdır.
	Name string

	// Status, ürünün durumudur (import'ta "active").
	Status string

	// AttributeValues, model düzeyi seçimlerdir.
	AttributeValues []CatalogSelectionInput

	// Variants, eksen tanımlarıdır.
	Variants []CatalogVariantAxisInput

	// Items, kalemlerdir.
	Items []CatalogProductItemInput

	// Splits, slicer değeri başına geçersiz kılmalardır.
	Splits []CatalogSplitInput

	// BrandID, marka kimliğidir; yoksa nil.
	BrandID *uuid.UUID

	// Description, ürün açıklamasıdır; yoksa nil.
	Description *string
}

// CreatedProductSnapshot, oluşturulan ürünün kimlik özetidir.
type CreatedProductSnapshot struct {
	// ProductID, oluşturulan ürünün kimliğidir.
	ProductID uuid.UUID

	// ItemIDByBarcode, barkod → kalem kimliği eşlemesidir (büyük/küçük harf
	// duyarsız kullanılmalıdır; anahtarlar barkodun özgün halidir).
	ItemIDByBarcode map[string]uuid.UUID
}

// EnsuredVariantSnapshot, garanti edilen varyant ekseninin özetidir.
type EnsuredVariantSnapshot struct {
	// ID, eksenin catalog kimliğidir.
	ID uuid.UUID

	// Name, eksenin adıdır.
	Name string

	// IsColor, eksenin renk stiliyle açıldığıdır.
	IsColor bool

	// Slicer, eksenin slicer olduğudur.
	Slicer bool

	// SlicerDemoted, slicer isteği mevcut başka bir slicer nedeniyle
	// düşürüldüyse true olur.
	SlicerDemoted bool
}

// CatalogImportGateway, import hattının Catalog yazma kapısıdır
// (.NET ICatalogImportGateway karşılığı). Tüm işlemler idempotenttir; tenant
// her çağrıda açık parametredir (Go'da ambient tenant bağlamı yoktur).
type CatalogImportGateway interface {
	// EnsureCategoryPath, ad zincirini kategori ağacında garanti eder ve
	// yaprağın kimliğini döner.
	EnsureCategoryPath(ctx context.Context, tenantID uuid.UUID, pathSegments []string) sharedkernel.ResultOf[uuid.UUID]

	// CategoryExists, kategorinin var olup olmadığını döner.
	CategoryExists(ctx context.Context, tenantID, categoryID uuid.UUID) (bool, error)

	// EnsureBrand, markayı ada göre garanti eder ve kimliğini döner.
	EnsureBrand(ctx context.Context, tenantID uuid.UUID, name string, externalID *string) sharedkernel.ResultOf[uuid.UUID]

	// EnsureAttribute, özelliği ada göre garanti eder ve kimliğini döner.
	EnsureAttribute(ctx context.Context, tenantID uuid.UUID, name string) sharedkernel.ResultOf[uuid.UUID]

	// EnsureAttributeValue, özellik değerini ada göre garanti eder.
	EnsureAttributeValue(ctx context.Context, tenantID, attributeID uuid.UUID, valueName string) sharedkernel.ResultOf[uuid.UUID]

	// EnsureVariant, varyant eksenini ada göre garanti eder; tenant başına tek
	// slicer kuralını uygular.
	EnsureVariant(ctx context.Context, tenantID uuid.UUID, name string, isColor, slicer bool) sharedkernel.ResultOf[EnsuredVariantSnapshot]

	// EnsureVariantValue, eksen değerini etikete (ya da aynı anahtara inen
	// mevcut değere) göre garanti eder.
	EnsureVariantValue(ctx context.Context, tenantID, variantID uuid.UUID, label string) sharedkernel.ResultOf[uuid.UUID]

	// AssignAttributeToCategory, özelliği kategoriye atar; mevcut atamanın
	// seviyesini yalnızca yükseltir (model → slicer/kalem).
	AssignAttributeToCategory(ctx context.Context, tenantID, categoryID, attributeID uuid.UUID, required bool, sortOrder int, scope PlannedAttributeScope) sharedkernel.Result

	// ProductGroupExists, model kodu ya da barkodlardan biri katalogda varsa
	// true döner (grup daha önce import edilmiş).
	ProductGroupExists(ctx context.Context, tenantID uuid.UUID, modelCode string, barcodes []string) (bool, error)

	// ResolveItemIDsByBarcode, barkodları mevcut kalem kimliklerine çözer.
	ResolveItemIDsByBarcode(ctx context.Context, tenantID uuid.UUID, barcodes []string) (map[string]uuid.UUID, error)

	// CreateProductsBatch, ürün grubunu oluşturur; kalem başına temel fiyat ve
	// stok da yazılır (dual-write).
	CreateProductsBatch(ctx context.Context, tenantID uuid.UUID, input CatalogProductBatchInput) sharedkernel.ResultOf[[]CreatedProductSnapshot]

	// EnsurePriceDefinition, fiyat tanımını ada göre garanti eder.
	EnsurePriceDefinition(ctx context.Context, tenantID uuid.UUID, name string, code *string) sharedkernel.ResultOf[uuid.UUID]

	// UpsertItemPrice, kalemin tanım bazlı fiyatını yazar/günceller.
	UpsertItemPrice(ctx context.Context, tenantID, productItemID, priceDefinitionID uuid.UUID, amount string, currency *string) sharedkernel.Result

	// AddProductImages, harici görselleri indirip medya deposuna alır ve ürüne
	// sırayla bağlar (ilk görsel birincil). İndirme sınırlı eşzamanlılıkla
	// yapılır; ilk hatada durur ve hatayı döner.
	AddProductImages(ctx context.Context, tenantID, productID uuid.UUID, sourceURLs []string) sharedkernel.Result
}
