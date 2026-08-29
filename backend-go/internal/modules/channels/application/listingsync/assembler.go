package listingsync

import (
	"context"
	"crypto/sha256"
	"encoding/hex"
	"sort"
	"strconv"
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/domain"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Assembler, Catalog kaynak verisinden pazaryerine gönderilecek listeleme
// isteğini kurar (.NET ListingAssembler portu): kategori + özellik/varyant
// eşlemelerini çözer, eşlemesi olmayan özellikleri sessizce düşürür.
type Assembler struct {
	store Store
}

// NewAssembler, depoyla assembler oluşturur.
func NewAssembler(store Store) *Assembler {
	return &Assembler{store: store}
}

// Assemble, kaynak veriyi + kararlaştırılmış fiyatı + stok miktarını
// pazaryeri isteğine dönüştürür (.NET AssembleAsync portu).
//
// Ön koşullar: kategori eşlemesi ve barkod zorunludur — eksikse hata döner ve
// kalem hiç gönderilmez. Özellik eşlemesi eksikse yalnızca o özellik payload'dan
// düşer (tüm kalem reddedilmez).
func (a *Assembler) Assemble(
	ctx context.Context,
	tenantID uuid.UUID,
	marketplaceCode string,
	source application.CatalogListingSource,
	price DecidedChannelPrice,
	quantity int,
) sharedkernel.ResultOf[MarketplaceListingRequest] {
	externalCategoryID, err := a.store.ResolveExternalCategoryID(ctx, tenantID, marketplaceCode, source.CategoryID)
	if err != nil {
		return sharedkernel.FailOf[MarketplaceListingRequest](sharedkernel.NewInternalError(err.Error()))
	}
	if externalCategoryID == nil || strings.TrimSpace(*externalCategoryID) == "" {
		return sharedkernel.FailOf[MarketplaceListingRequest](sharedkernel.NewValidationError(
			"Ürünün kategorisi pazaryeri kategorisine eşlenmemiş."))
	}
	if strings.TrimSpace(source.Barcode) == "" {
		return sharedkernel.FailOf[MarketplaceListingRequest](sharedkernel.NewValidationError(
			"Kalemin barkodu yok; pazaryerinde kimliklendirilemez."))
	}

	attributes := []MarketplaceListingAttribute{}
	for _, selection := range source.Attributes {
		mapped, err := a.mapSelection(ctx, tenantID, marketplaceCode, source.CategoryID, selection)
		if err != nil {
			return sharedkernel.FailOf[MarketplaceListingRequest](sharedkernel.NewInternalError(err.Error()))
		}
		if mapped != nil {
			attributes = append(attributes, *mapped)
		}
	}

	return sharedkernel.OkOf(MarketplaceListingRequest{
		ProductItemID: source.ProductItemID, Barcode: source.Barcode, Title: source.Title,
		Description: source.Description, ExternalCategoryID: *externalCategoryID,
		BrandExternalID: source.BrandExternalCode, BrandName: source.BrandName,
		ModelCode: source.ModelCode, Sku: source.Sku,
		Amount: price.Amount, CompareAtAmount: price.CompareAtAmount, Currency: price.Currency,
		Quantity: quantity, Attributes: attributes, ImageURLs: source.ImageURLs,
	})
}

// mapSelection, tek özellik/varyant seçimini pazaryeri karşılığına çözer;
// eşlemesi yoksa nil döner (özellik payload'dan düşer, hata sayılmaz).
func (a *Assembler) mapSelection(
	ctx context.Context,
	tenantID uuid.UUID,
	marketplaceCode string,
	categoryID uuid.UUID,
	selection application.CatalogListingSelection,
) (*MarketplaceListingAttribute, error) {
	sourceType := domain.SourceCatalogAttribute
	if selection.IsVariant {
		sourceType = domain.SourceCatalogVariant
	}
	mapping, err := a.store.GetAttributeMapping(ctx, tenantID, marketplaceCode, categoryID, sourceType, selection.SourceID)
	if err != nil {
		return nil, err
	}
	if mapping == nil {
		return nil, nil
	}
	valueMapping, err := a.store.GetValueMapping(ctx, tenantID, mapping.ID, selection.ValueID)
	if err != nil {
		return nil, err
	}
	if valueMapping == nil {
		// Eşlenmiş değer yok: serbest metin olarak etiket gönderilir.
		return &MarketplaceListingAttribute{
			ExternalAttributeID: mapping.ExternalAttributeID, CustomValue: &selection.ValueLabel}, nil
	}
	return &MarketplaceListingAttribute{
		ExternalAttributeID: mapping.ExternalAttributeID, ExternalValueID: &valueMapping.ExternalValueID}, nil
}

// ComputeContentHash, içerik hash'ini hesaplar (.NET ContentHasher.Compute
// portu). Fiyat ve stok BİLİNÇLİ OLARAK hash'e dahil değildir — teklif
// senkronu ayrı bir akıştır ve içeriği yeniden onaya sokmamalıdır.
//
// Kanonik dizgi: Barcode|Title|Description|ExternalCategoryId|BrandExternalId|
// BrandName|ModelCode|Sku|attr1Id=attr1Value;attr2Id=attr2Value;...|img1;img2;...
// Özellikler ExternalAttributeID'ye göre Ordinal artan sıraya konur (kararlılık
// için); görseller kaynak sırasıyla kalır.
func ComputeContentHash(listing MarketplaceListingRequest) string {
	var b strings.Builder
	b.WriteString(listing.Barcode)
	b.WriteByte('|')
	b.WriteString(listing.Title)
	b.WriteByte('|')
	b.WriteString(deref(listing.Description))
	b.WriteByte('|')
	b.WriteString(listing.ExternalCategoryID)
	b.WriteByte('|')
	b.WriteString(deref(listing.BrandExternalID))
	b.WriteByte('|')
	b.WriteString(deref(listing.BrandName))
	b.WriteByte('|')
	b.WriteString(listing.ModelCode)
	b.WriteByte('|')
	b.WriteString(deref(listing.Sku))
	b.WriteByte('|')

	attributes := make([]MarketplaceListingAttribute, len(listing.Attributes))
	copy(attributes, listing.Attributes)
	sort.Slice(attributes, func(i, j int) bool {
		return attributes[i].ExternalAttributeID < attributes[j].ExternalAttributeID
	})
	for _, attribute := range attributes {
		b.WriteString(attribute.ExternalAttributeID)
		b.WriteByte('=')
		if attribute.ExternalValueID != nil {
			b.WriteString(*attribute.ExternalValueID)
		} else {
			b.WriteString(deref(attribute.CustomValue))
		}
		b.WriteByte(';')
	}

	b.WriteByte('|')
	for _, imageURL := range listing.ImageURLs {
		b.WriteString(imageURL)
		b.WriteByte(';')
	}

	sum := sha256.Sum256([]byte(b.String()))
	return hex.EncodeToString(sum[:])
}

// ComputeOfferHash, teklif hash'ini hesaplar (.NET OfferHasher.Compute portu).
//
// Kanonik dizgi: ExternalListingId|Quantity|Amount|CompareAtAmount veya ""|Currency
func ComputeOfferHash(offer MarketplaceOfferUpdate) string {
	compareAt := ""
	if offer.CompareAtAmount != nil {
		compareAt = *offer.CompareAtAmount
	}
	canonical := strings.Join([]string{
		offer.ExternalListingID, strconv.Itoa(offer.Quantity), offer.Amount, compareAt, offer.Currency,
	}, "|")
	sum := sha256.Sum256([]byte(canonical))
	return hex.EncodeToString(sum[:])
}

// deref, işaretçiyi güvenle çözer; nil ise boş dizgi döner.
func deref(value *string) string {
	if value == nil {
		return ""
	}
	return *value
}
