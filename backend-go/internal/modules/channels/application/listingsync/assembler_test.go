package listingsync

// Assembler ve hash fonksiyonları için birim testleri (.NET ListingAssembler +
// ContentHasher + OfferHasher davranışlarının doğrulanması): kategori/barkod
// ön koşulları, eşlemesi olmayan özelliklerin sessizce düşmesi, hash'in
// fiyat/stoktan bağımsız olması ve özellik sırasından etkilenmemesi.

import (
	"context"
	"testing"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/channels/application"
	"pimly.commerslab/backend-go/internal/modules/channels/domain"
)

// mockStore, Assembler testleri için yalnızca eşleme çözme metodlarını
// uygular; diğer Store metodları çağrılmayacağından panic eder.
type mockStore struct {
	resolveExternalCategoryID func(tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID) (*string, error)
	getAttributeMapping       func(tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID, sourceType domain.AttributeMappingSourceType, catalogSourceID uuid.UUID) (*domain.AttributeChannelMapping, error)
	getValueMapping           func(tenantID uuid.UUID, attributeMappingID, catalogValueID uuid.UUID) (*domain.AttributeValueChannelMapping, error)
}

func (m *mockStore) ListDirtyScopes(context.Context, []uuid.UUID, time.Time) ([]domain.ListingSyncScope, error) {
	panic("not implemented")
}
func (m *mockStore) ListDirty(context.Context, uuid.UUID, string, time.Time, int) ([]*domain.ProductListing, error) {
	panic("not implemented")
}
func (m *mockStore) Update(context.Context, *domain.ProductListing) error { panic("not implemented") }
func (m *mockStore) GetConnection(context.Context, uuid.UUID, string) (*domain.MarketplaceConnection, error) {
	panic("not implemented")
}
func (m *mockStore) ResolveExternalCategoryID(_ context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID) (*string, error) {
	return m.resolveExternalCategoryID(tenantID, marketplaceCode, catalogCategoryID)
}
func (m *mockStore) GetAttributeMapping(_ context.Context, tenantID uuid.UUID, marketplaceCode string, catalogCategoryID uuid.UUID, sourceType domain.AttributeMappingSourceType, catalogSourceID uuid.UUID) (*domain.AttributeChannelMapping, error) {
	if m.getAttributeMapping == nil {
		return nil, nil
	}
	return m.getAttributeMapping(tenantID, marketplaceCode, catalogCategoryID, sourceType, catalogSourceID)
}
func (m *mockStore) GetValueMapping(_ context.Context, tenantID uuid.UUID, attributeMappingID, catalogValueID uuid.UUID) (*domain.AttributeValueChannelMapping, error) {
	if m.getValueMapping == nil {
		return nil, nil
	}
	return m.getValueMapping(tenantID, attributeMappingID, catalogValueID)
}

func strPtr(v string) *string { return &v }

func TestAssemble_MissingCategoryMapping_Fails(t *testing.T) {
	store := &mockStore{
		resolveExternalCategoryID: func(uuid.UUID, string, uuid.UUID) (*string, error) { return nil, nil },
	}
	a := NewAssembler(store)
	source := application.CatalogListingSource{
		ProductItemID: uuid.New(), CategoryID: uuid.New(), Title: "Ürün", ModelCode: "M1", Barcode: "BC1"}
	result := a.Assemble(context.Background(), uuid.New(), "TY", source, DecidedChannelPrice{Amount: "10.00", Currency: "TRY"}, 5)
	if result.IsSuccess() {
		t.Fatal("kategori eşlemesi yokken başarı bekleniyordu değil")
	}
}

func TestAssemble_MissingBarcode_Fails(t *testing.T) {
	store := &mockStore{
		resolveExternalCategoryID: func(uuid.UUID, string, uuid.UUID) (*string, error) {
			return strPtr("221"), nil
		},
	}
	a := NewAssembler(store)
	source := application.CatalogListingSource{
		ProductItemID: uuid.New(), CategoryID: uuid.New(), Title: "Ürün", ModelCode: "M1", Barcode: ""}
	result := a.Assemble(context.Background(), uuid.New(), "TY", source, DecidedChannelPrice{Amount: "10.00", Currency: "TRY"}, 5)
	if result.IsSuccess() {
		t.Fatal("barkod yokken başarı bekleniyordu değil")
	}
}

func TestAssemble_UnmappedAttribute_DroppedNotFailed(t *testing.T) {
	store := &mockStore{
		resolveExternalCategoryID: func(uuid.UUID, string, uuid.UUID) (*string, error) { return strPtr("221"), nil },
		getAttributeMapping: func(uuid.UUID, string, uuid.UUID, domain.AttributeMappingSourceType, uuid.UUID) (*domain.AttributeChannelMapping, error) {
			return nil, nil // eşleme yok
		},
	}
	a := NewAssembler(store)
	source := application.CatalogListingSource{
		ProductItemID: uuid.New(), CategoryID: uuid.New(), Title: "Ürün", ModelCode: "M1", Barcode: "BC1",
		Attributes: []application.CatalogListingSelection{
			{IsVariant: false, SourceID: uuid.New(), ValueID: uuid.New(), ValueLabel: "Kırmızı"},
		},
	}
	result := a.Assemble(context.Background(), uuid.New(), "TY", source, DecidedChannelPrice{Amount: "10.00", Currency: "TRY"}, 5)
	if result.IsFailure() {
		t.Fatalf("eşlemesi olmayan özellik tüm kalemi reddetmemeliydi: %v", result.Err())
	}
	if len(result.Value().Attributes) != 0 {
		t.Fatalf("eşlemesi olmayan özellik payload'a girmemeliydi: %+v", result.Value().Attributes)
	}
}

func TestAssemble_MappedValue_UsesExternalValueID(t *testing.T) {
	attributeMappingID := uuid.New()
	store := &mockStore{
		resolveExternalCategoryID: func(uuid.UUID, string, uuid.UUID) (*string, error) { return strPtr("221"), nil },
		getAttributeMapping: func(uuid.UUID, string, uuid.UUID, domain.AttributeMappingSourceType, uuid.UUID) (*domain.AttributeChannelMapping, error) {
			return &domain.AttributeChannelMapping{ID: attributeMappingID, ExternalAttributeID: "47"}, nil
		},
		getValueMapping: func(uuid.UUID, uuid.UUID, uuid.UUID) (*domain.AttributeValueChannelMapping, error) {
			return &domain.AttributeValueChannelMapping{ExternalValueID: "686"}, nil
		},
	}
	a := NewAssembler(store)
	source := application.CatalogListingSource{
		ProductItemID: uuid.New(), CategoryID: uuid.New(), Title: "Ürün", ModelCode: "M1", Barcode: "BC1",
		Attributes: []application.CatalogListingSelection{
			{IsVariant: true, SourceID: uuid.New(), ValueID: uuid.New(), ValueLabel: "Mavi"},
		},
	}
	result := a.Assemble(context.Background(), uuid.New(), "TY", source, DecidedChannelPrice{Amount: "10.00", Currency: "TRY"}, 5)
	if result.IsFailure() {
		t.Fatalf("beklenmeyen hata: %v", result.Err())
	}
	attrs := result.Value().Attributes
	if len(attrs) != 1 || attrs[0].ExternalAttributeID != "47" || attrs[0].ExternalValueID == nil || *attrs[0].ExternalValueID != "686" {
		t.Fatalf("eşlenmiş değer beklenenden farklı: %+v", attrs)
	}
}

func TestAssemble_UnmappedValue_FallsBackToCustomValue(t *testing.T) {
	attributeMappingID := uuid.New()
	store := &mockStore{
		resolveExternalCategoryID: func(uuid.UUID, string, uuid.UUID) (*string, error) { return strPtr("221"), nil },
		getAttributeMapping: func(uuid.UUID, string, uuid.UUID, domain.AttributeMappingSourceType, uuid.UUID) (*domain.AttributeChannelMapping, error) {
			return &domain.AttributeChannelMapping{ID: attributeMappingID, ExternalAttributeID: "47"}, nil
		},
		getValueMapping: func(uuid.UUID, uuid.UUID, uuid.UUID) (*domain.AttributeValueChannelMapping, error) {
			return nil, nil // değer eşlemesi yok
		},
	}
	a := NewAssembler(store)
	source := application.CatalogListingSource{
		ProductItemID: uuid.New(), CategoryID: uuid.New(), Title: "Ürün", ModelCode: "M1", Barcode: "BC1",
		Attributes: []application.CatalogListingSelection{
			{IsVariant: true, SourceID: uuid.New(), ValueID: uuid.New(), ValueLabel: "Turkuaz"},
		},
	}
	result := a.Assemble(context.Background(), uuid.New(), "TY", source, DecidedChannelPrice{Amount: "10.00", Currency: "TRY"}, 5)
	if result.IsFailure() {
		t.Fatalf("beklenmeyen hata: %v", result.Err())
	}
	attrs := result.Value().Attributes
	if len(attrs) != 1 || attrs[0].ExternalValueID != nil || attrs[0].CustomValue == nil || *attrs[0].CustomValue != "Turkuaz" {
		t.Fatalf("serbest metin geri dönüşü beklenenden farklı: %+v", attrs)
	}
}

func sampleListingRequest() MarketplaceListingRequest {
	return MarketplaceListingRequest{
		Barcode: "BC1", Title: "Klasik Gömlek", Description: strPtr("Açıklama"),
		ExternalCategoryID: "221", BrandExternalID: strPtr("55"), BrandName: strPtr("Pimly"),
		ModelCode: "M1", Sku: strPtr("SKU1"), Amount: "449.90", Currency: "TRY", Quantity: 10,
		Attributes: []MarketplaceListingAttribute{
			{ExternalAttributeID: "47", ExternalValueID: strPtr("686")},
			{ExternalAttributeID: "12", CustomValue: strPtr("Keten")},
		},
		ImageURLs: []string{"https://cdn/1.jpg", "https://cdn/2.jpg"},
	}
}

func TestComputeContentHash_StableUnderAttributeReordering(t *testing.T) {
	a := sampleListingRequest()
	b := sampleListingRequest()
	b.Attributes = []MarketplaceListingAttribute{a.Attributes[1], a.Attributes[0]}
	if ComputeContentHash(a) != ComputeContentHash(b) {
		t.Fatal("özellik sırası hash'i değiştirmemeliydi")
	}
}

func TestComputeContentHash_IgnoresPriceAndQuantity(t *testing.T) {
	a := sampleListingRequest()
	b := sampleListingRequest()
	b.Amount = "999.00"
	b.CompareAtAmount = strPtr("1200.00")
	b.Quantity = 0
	if ComputeContentHash(a) != ComputeContentHash(b) {
		t.Fatal("fiyat/stok içerik hash'ine dahil edilmemeliydi")
	}
}

func TestComputeContentHash_ChangesWithTitle(t *testing.T) {
	a := sampleListingRequest()
	b := sampleListingRequest()
	b.Title = "Farklı Başlık"
	if ComputeContentHash(a) == ComputeContentHash(b) {
		t.Fatal("başlık değişince hash de değişmeliydi")
	}
}

func TestComputeContentHash_Is64CharLowercaseHex(t *testing.T) {
	hash := ComputeContentHash(sampleListingRequest())
	if len(hash) != 64 {
		t.Fatalf("64 karakter hex bekleniyordu, %d bulundu", len(hash))
	}
	for _, ch := range hash {
		if !((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f')) {
			t.Fatalf("küçük harf hex bekleniyordu: %q", hash)
		}
	}
}

func TestComputeOfferHash_Deterministic(t *testing.T) {
	offer := MarketplaceOfferUpdate{ExternalListingID: "BC1", Quantity: 10, Amount: "449.90", Currency: "TRY"}
	if ComputeOfferHash(offer) != ComputeOfferHash(offer) {
		t.Fatal("aynı girdi aynı hash'i üretmeliydi")
	}
}

func TestComputeOfferHash_ChangesWithQuantity(t *testing.T) {
	a := MarketplaceOfferUpdate{ExternalListingID: "BC1", Quantity: 10, Amount: "449.90", Currency: "TRY"}
	b := a
	b.Quantity = 11
	if ComputeOfferHash(a) == ComputeOfferHash(b) {
		t.Fatal("miktar değişince hash de değişmeliydi")
	}
}

func TestComputeOfferHash_NilCompareAtTreatedAsEmpty(t *testing.T) {
	withNil := MarketplaceOfferUpdate{ExternalListingID: "BC1", Quantity: 10, Amount: "449.90", Currency: "TRY"}
	withEmpty := withNil
	empty := ""
	withEmpty.CompareAtAmount = &empty
	if ComputeOfferHash(withNil) != ComputeOfferHash(withEmpty) {
		t.Fatal("nil ve boş dizgi CompareAtAmount aynı hash'i üretmeliydi")
	}
}

func TestBackoffDelay_CapsAtMax(t *testing.T) {
	delay := backoffDelay(50, 10, offerBaseBackoff, offerMaxBackoff)
	if delay != offerMaxBackoff {
		t.Fatalf("çok yüksek deneme sayısında tavana çarpması bekleniyordu: %v", delay)
	}
}

func TestBackoffDelay_GrowsExponentially(t *testing.T) {
	d0 := backoffDelay(0, 10, offerBaseBackoff, offerMaxBackoff)
	d1 := backoffDelay(1, 10, offerBaseBackoff, offerMaxBackoff)
	d2 := backoffDelay(2, 10, offerBaseBackoff, offerMaxBackoff)
	if d0 != offerBaseBackoff {
		t.Fatalf("0. denemede taban süre bekleniyordu: %v", d0)
	}
	if d1 != 2*offerBaseBackoff || d2 != 4*offerBaseBackoff {
		t.Fatalf("üstel artış beklenenden farklı: d1=%v d2=%v", d1, d2)
	}
}
