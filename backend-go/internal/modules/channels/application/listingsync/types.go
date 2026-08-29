// Package listingsync, kirli listelemelerin fiyat/stok ve içerik bilgisini
// pazaryerlerine gönderen senkron mantığını içerir (.NET Channels.Application/
// Listings/OfferSync + ContentSync karşılığı). Olay başına push yerine
// debounce edilmiş toplu tur kullanılır: bir kalemin ardışık değişimleri tek
// gönderime iner.
//
// İki ayrı akış vardır: teklif senkronu (ucuz, onaysız — fiyat/stok) ve içerik
// senkronu (pahalı, yeniden onaya sokar — başlık/özellik/görsel). Debounce
// penceresi boyunca biriken kirlilik her turda hash'e karşı süzülür; hash
// değişmemişse pazaryerine hiç çağrı yapılmaz.
package listingsync

import (
	"context"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// --- Pazaryeri teklif istemcisi (.NET IMarketplaceOfferClient) ---

// MarketplaceOfferUpdate, pazaryerine gönderilecek tek teklif güncellemesidir.
type MarketplaceOfferUpdate struct {
	// ExternalListingID, pazaryerindeki listeleme kimliğidir (Trendyol'da barkod).
	ExternalListingID string

	// Quantity, satılabilir stok adedidir.
	Quantity int

	// Amount, satış fiyatının ham ondalık dizgisidir.
	Amount string

	// CompareAtAmount, üstü çizili fiyattır; yoksa nil.
	CompareAtAmount *string

	// Currency, para birimi kodudur.
	Currency string
}

// OfferUpdateReceipt, teklif gönderiminin makbuzudur.
type OfferUpdateReceipt struct {
	// SubmissionReference, pazaryerinin döndürdüğü iş takip kimliğidir (ör.
	// Trendyol batchRequestId); yoksa nil.
	SubmissionReference *string
}

// MarketplaceOfferClient, pazaryerine toplu teklif (fiyat/stok) gönderen porttur.
type MarketplaceOfferClient interface {
	// MaxBatchSize, tek istekte gönderilebilecek en fazla teklif sayısıdır.
	MaxBatchSize() int

	// UpdateOffers, teklif partisini gönderir; boş parti HTTP çağrısı yapmadan başarı döner.
	UpdateOffers(ctx context.Context, credentials *application.MarketplaceCredentials, offers []MarketplaceOfferUpdate) sharedkernel.ResultOf[OfferUpdateReceipt]
}

// --- Pazaryeri listeleme (içerik) istemcisi (.NET IMarketplaceListingClient) ---

// MarketplaceListingAttribute, listeleme isteğindeki tek özellik değeridir.
type MarketplaceListingAttribute struct {
	// ExternalAttributeID, özelliğin pazaryeri kimliğidir.
	ExternalAttributeID string

	// ExternalValueID, eşlenmiş değerin pazaryeri kimliğidir; serbest metinde nil.
	ExternalValueID *string

	// CustomValue, serbest metin değeridir; eşlenmiş değerde nil.
	CustomValue *string
}

// MarketplaceListingRequest, pazaryerine gönderilecek tek ürün kartıdır.
type MarketplaceListingRequest struct {
	// ProductItemID, kalemin Pimly kimliğidir (pazaryerine gönderilmez, iç referans).
	ProductItemID uuid.UUID

	// Barcode, kalemin barkodudur.
	Barcode string

	// Title, ürün başlığıdır.
	Title string

	// Description, ürün açıklamasıdır; yoksa nil.
	Description *string

	// ExternalCategoryID, pazaryeri kategori kimliğidir.
	ExternalCategoryID string

	// BrandExternalID, pazaryeri marka kimliğidir; yoksa nil.
	BrandExternalID *string

	// BrandName, marka adıdır; yoksa nil.
	BrandName *string

	// ModelCode, varyant grubunu birleştiren model kodudur (Trendyol productMainId).
	ModelCode string

	// Sku, kalemin stok kodudur; yoksa nil.
	Sku *string

	// Amount, satış fiyatının ham ondalık dizgisidir.
	Amount string

	// CompareAtAmount, üstü çizili fiyattır; yoksa nil.
	CompareAtAmount *string

	// Currency, para birimi kodudur.
	Currency string

	// Quantity, satılabilir stok adedidir.
	Quantity int

	// Attributes, kartın özellik değerleridir.
	Attributes []MarketplaceListingAttribute

	// ImageURLs, kartın görsel bağlantılarıdır (sıralı; birincil önce).
	ImageURLs []string
}

// ListingSubmissionReceipt, içerik gönderiminin makbuzudur.
type ListingSubmissionReceipt struct {
	// SubmissionReference, pazaryerinin döndürdüğü iş takip kimliğidir; yoksa nil.
	SubmissionReference *string
}

// MarketplaceListingClient, pazaryerine toplu ürün kartı gönderen porttur.
type MarketplaceListingClient interface {
	// MaxBatchSize, tek istekte gönderilebilecek en fazla kart sayısıdır.
	MaxBatchSize() int

	// Submit, kart partisini gönderir; isUpdate true ise güncelleme ucu
	// (PUT), false ise yeni kart ucu (POST) kullanılır. Boş parti HTTP
	// çağrısı yapmadan başarı döner.
	Submit(ctx context.Context, credentials *application.MarketplaceCredentials, listings []MarketplaceListingRequest, isUpdate bool) sharedkernel.ResultOf[ListingSubmissionReceipt]
}

// --- Kaynak veri kapıları (.NET gateway arayüzleri) ---

// DecidedChannelPrice, kalemin bir pazaryeri için açıkça belirlenmiş
// (satıcının SetChannelPrice ile girdiği) fiyatıdır — base price'tan
// otomatik türetilmez; kanal fiyatı yoksa kalem senkronlanmaz.
type DecidedChannelPrice struct {
	ProductItemID   uuid.UUID
	Amount          string
	CompareAtAmount *string
	Currency        string
}

// PricingChannelPriceGateway, pazaryerindeki tüm kanal fiyatlarını okuyan porttur.
type PricingChannelPriceGateway interface {
	// ListForMarketplace, tenant'ın bir pazaryerindeki tüm kanal fiyatlarını döner.
	ListForMarketplace(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) ([]DecidedChannelPrice, error)
}

// InventoryStockGateway, kalemlerin stok miktarlarını okuyan porttur.
type InventoryStockGateway interface {
	// GetQuantities, verilen kalemlerin miktarlarını döner; kaydı olmayan
	// kalem haritada yer almaz (çağıran taraf 0 sayar).
	GetQuantities(ctx context.Context, tenantID uuid.UUID, productItemIDs []uuid.UUID) (map[uuid.UUID]int, error)
}

// CatalogListingSourceGateway, kalemlerin pazaryerine giden içerik
// kaynaklarını (başlık/özellik/görsel) okuyan porttur.
type CatalogListingSourceGateway interface {
	// Get, verilen kalemlerin içerik kaynaklarını döner.
	Get(ctx context.Context, tenantID uuid.UUID, productItemIDs []uuid.UUID) ([]application.CatalogListingSource, error)
}

// Store, senkron akışlarının ihtiyaç duyduğu Channels kalıcılık yüzeyidir;
// somut karşılığı channels/infrastructure.Repository + ListingRepository'dir.
type Store interface {
	// ListDirtyScopes, gönderim bekleyen (tenant, pazaryeri) çiftlerini keşfeder.
	ListDirtyScopes(ctx context.Context, tenantFilter []uuid.UUID, now time.Time) ([]domain.ListingSyncScope, error)

	// ListDirty, kapsamdaki gönderim bekleyen listelemeleri döner (backoff dolmamışlar hariç).
	ListDirty(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, now time.Time, limit int) ([]*domain.ProductListing, error)

	// Update, listelemeyi kalıcılaştırır.
	Update(ctx context.Context, listing *domain.ProductListing) error

	// GetConnection, tenant'ın pazaryeri bağlantısını döner; yoksa nil.
	GetConnection(ctx context.Context, tenantID uuid.UUID, marketplaceCode string) (*domain.MarketplaceConnection, error)

	// ResolveExternalCategoryID, catalog kategorisinin eşlenen harici kimliğini döner; yoksa nil.
	ResolveExternalCategoryID(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID) (*string, error)

	// GetAttributeMapping, doğal anahtarla alan eşlemesini döner; yoksa nil.
	GetAttributeMapping(ctx context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID, sourceType domain.AttributeMappingSourceType, catalogSourceID uuid.UUID) (*domain.AttributeChannelMapping, error)

	// GetValueMapping, doğal anahtarla değer eşlemesini döner; yoksa nil.
	GetValueMapping(ctx context.Context, tenantID uuid.UUID, attributeMappingID, catalogValueID uuid.UUID) (*domain.AttributeValueChannelMapping, error)
}

// ClientResolver, pazaryeri koduna göre teklif/içerik istemcisi çözen basit
// bir map tabanlı registry'dir (.NET keyed-DI resolver karşılığı).
type ClientResolver struct {
	offerClients   map[string]MarketplaceOfferClient
	listingClients map[string]MarketplaceListingClient
}

// NewClientResolver, boş bir resolver oluşturur; RegisterOffer/RegisterListing
// ile pazaryeri başına istemci kaydedilir.
func NewClientResolver() *ClientResolver {
	return &ClientResolver{
		offerClients:   map[string]MarketplaceOfferClient{},
		listingClients: map[string]MarketplaceListingClient{},
	}
}

// RegisterOffer, pazaryeri kodu için teklif istemcisi kaydeder.
func (r *ClientResolver) RegisterOffer(marketplaceCode string, client MarketplaceOfferClient) {
	r.offerClients[marketplaceCode] = client
}

// RegisterListing, pazaryeri kodu için içerik istemcisi kaydeder.
func (r *ClientResolver) RegisterListing(marketplaceCode string, client MarketplaceListingClient) {
	r.listingClients[marketplaceCode] = client
}

// ResolveOffer, pazaryeri için teklif istemcisini döner; kayıtlı değilse not_found.
func (r *ClientResolver) ResolveOffer(marketplaceCode string) sharedkernel.ResultOf[MarketplaceOfferClient] {
	client, ok := r.offerClients[marketplaceCode]
	if !ok {
		return sharedkernel.FailOf[MarketplaceOfferClient](sharedkernel.NewNotFoundError(
			"Offer client is not configured for marketplace '" + marketplaceCode + "'."))
	}
	return sharedkernel.OkOf(client)
}

// ResolveListing, pazaryeri için içerik istemcisini döner; kayıtlı değilse not_found.
func (r *ClientResolver) ResolveListing(marketplaceCode string) sharedkernel.ResultOf[MarketplaceListingClient] {
	client, ok := r.listingClients[marketplaceCode]
	if !ok {
		return sharedkernel.FailOf[MarketplaceListingClient](sharedkernel.NewNotFoundError(
			"Listing client is not configured for marketplace '" + marketplaceCode + "'."))
	}
	return sharedkernel.OkOf(client)
}

// --- Tur sonuçları ---

// OfferSyncSummary, bir teklif senkron turunun özetidir.
type OfferSyncSummary struct {
	Examined int // İncelenen kirli listeleme sayısı.
	Skipped  int // Hash aynı olduğu için çağrı yapılmadan atlanan sayısı.
	Pushed   int // Pazaryerine başarıyla gönderilen sayısı.
	Failed   int // Gönderimi başarısız olan sayısı.
}

// ContentSyncSummary, bir içerik senkron turunun özetidir.
type ContentSyncSummary struct {
	Examined int // İncelenen kirli listeleme sayısı.
	Skipped  int // Hash aynı veya ön koşul eksik olduğu için atlanan sayısı.
	Created  int // Pazaryerinde yeni kart olarak gönderilen sayısı.
	Updated  int // Mevcut kartı güncellenen sayısı.
	Failed   int // Gönderimi başarısız olan sayısı.
}
