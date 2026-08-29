// Package products, ürün kök aggregate'ini ve satılabilir kalemlerini içerir
// (.NET Catalog.Domain.Products karşılığı). Product; model kodu, ad, durum,
// ürün düzeyinde özellik değerleri ve eksen tanımlarını, her satılabilir
// kombinasyonu ProductItem olarak tutar. Bütünleşme olayları aggregate üzerinde
// biriktirilir ve kalıcılık katmanı tarafından aynı transaction'da outbox'a yazılır.
package products

import (
	"strings"
	"time"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/outbox"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Status, ürün yaşam döngüsü durumudur; kabloda ve veritabanında küçük harfli
// dizgi taşınır ("draft"/"active"/"archived").
type Status string

// Ürün durumları.
const (
	StatusDraft    Status = "draft"
	StatusActive   Status = "active"
	StatusArchived Status = "archived"
)

// ParseStatus, kullanıcı girdisini duruma çözer (.NET Enum.Parse ignoreCase
// karşılığı); tanınmayan değer için ok=false döner.
func ParseStatus(value string) (Status, bool) {
	switch strings.ToLower(strings.TrimSpace(value)) {
	case string(StatusDraft):
		return StatusDraft, true
	case string(StatusActive):
		return StatusActive, true
	case string(StatusArchived):
		return StatusArchived, true
	default:
		return "", false
	}
}

// AttributeRef, jsonb'de saklanan özellik tanım anlık görüntüsüdür
// (.NET Products.Attribute kaydı; alan adları veritabanı belge biçiminin parçası).
type AttributeRef struct {
	ID   uuid.UUID `json:"id"`
	Key  string    `json:"key"`
	Name string    `json:"name"`
}

// AttributeValue, jsonb'de saklanan özellik değeri anlık görüntüsüdür.
type AttributeValue struct {
	Attribute AttributeRef `json:"attribute"`
	ID        uuid.UUID    `json:"id"`
	Name      string       `json:"name"`
}

// VariantRef, ürün oluşturulurken katalog varyant türünden alınan eksen tanım
// anlık görüntüsüdür (jsonb belge biçimi; selection_style "list"/"color").
type VariantRef struct {
	ID             uuid.UUID `json:"id"`
	Name           string    `json:"name"`
	SelectionStyle string    `json:"selection_style"`
	Slicer         bool      `json:"slicer"`
}

// VariantValue, kalem üzerindeki eksen değeri anlık görüntüsüdür (jsonb belge
// biçimi; key null olabilir ve .NET gibi her zaman yazılır).
type VariantValue struct {
	Variant VariantRef `json:"variant"`
	ID      uuid.UUID  `json:"id"`
	Name    string     `json:"name"`
	Key     *string    `json:"key"`
}

// ProductItem, ürün altında satılabilir SKU/barkod birimini temsil eder.
// Fiyat Pricing'e, stok Inventory'ye aittir.
type ProductItem struct {
	ID               uuid.UUID
	Sku              *string
	Barcode          string
	Gtin             *string
	Mpn              *string
	AxisValueEntryID *uuid.UUID
	AxisValue        *string
	AttributeValues  []AttributeValue
	VariantValues    []VariantValue
}

// ProductImage, ürün galerisindeki tek bir görseli temsil eder.
type ProductImage struct {
	ID             uuid.UUID
	URL            string
	SortOrder      int
	AltText        *string
	IsPrimary      bool
	VariantValueID *uuid.UUID
}

// ItemDraft, yeni satılabilir kalem oluşturma girdisidir (.NET ProductItemDraft).
type ItemDraft struct {
	Sku              *string
	Barcode          string
	Gtin             *string
	Mpn              *string
	AxisValueEntryID *uuid.UUID
	AxisValue        *string
	AttributeValues  []AttributeValue
	VariantValues    []VariantValue
}

// ItemUpdate, mevcut kalem güncelleme girdisidir. Barcode/Sku nil bırakılırsa
// mevcut değer korunur; boş Sku metni SKU'yu temizler (.NET ProductItemUpdate).
type ItemUpdate struct {
	Gtin             *string
	Mpn              *string
	AxisValueEntryID *uuid.UUID
	AxisValue        *string
	AttributeValues  []AttributeValue // nil = koru
	Sku              *string
	Barcode          *string
}

// Product, ürün kök aggregate'idir.
type Product struct {
	ID          uuid.UUID
	GroupID     uuid.UUID
	CategoryID  uuid.UUID
	BrandID     *uuid.UUID
	ModelCode   string
	GroupCode   *string
	SlicerValue *string
	Name        string
	Description *string
	Status      Status

	// VatRate, ürünün KDV oranıdır ("20.00" gibi tam ondalık metin — kod
	// tabanının kuralı gereği ondalıklar float tutulmaz). nil ise tenant
	// varsayılanı (catalog_settings.default_vat_rate) geçerlidir.
	VatRate *string
	AttributeValues []AttributeValue
	Variants        []VariantRef
	Items           []*ProductItem
	Images          []*ProductImage

	// pendingEvents, kalıcılık katmanının aynı transaction'da outbox'a
	// yazacağı bütünleşme olaylarıdır (.NET RaiseDomainEvent karşılığı).
	pendingEvents []outbox.Event
}

// PendingEvents, biriktirilmiş bütünleşme olaylarını döner.
func (p *Product) PendingEvents() []outbox.Event { return p.pendingEvents }

// ClearPendingEvents, olayları temizler; kalıcılık başarıyla tamamlandığında çağrılır.
func (p *Product) ClearPendingEvents() { p.pendingEvents = nil }

// raise, olayı biriktirir.
func (p *Product) raise(event outbox.Event) { p.pendingEvents = append(p.pendingEvents, event) }

// ValidateVariantStructure, eksen sayısı ile kalem sayısının ürün tipine uygun
// olup olmadığını doğrular: eksensiz (basit) ürün tam bir kalem, eksenli ürün
// en az bir kalem taşır; en çok 3 eksen olabilir.
func ValidateVariantStructure(variantCount, itemCount int) sharedkernel.Result {
	if variantCount < 0 || variantCount > 3 {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Product must have between 0 and 3 variant types."))
	}
	if variantCount == 0 {
		if itemCount == 1 {
			return sharedkernel.Ok()
		}
		return sharedkernel.Fail(sharedkernel.NewValidationError("Basic product must have exactly one variant."))
	}
	if itemCount >= 1 {
		return sharedkernel.Ok()
	}
	return sharedkernel.Fail(sharedkernel.NewValidationError("Variant product must have at least one variant."))
}

// newItem, taslak girdiden yeni kalem oluşturur (.NET ProductItem.Create).
func newItem(draft ItemDraft) sharedkernel.ResultOf[*ProductItem] {
	if strings.TrimSpace(draft.Barcode) == "" {
		return sharedkernel.FailOf[*ProductItem](sharedkernel.NewValidationError("Variant barcode is required."))
	}
	return sharedkernel.OkOf(&ProductItem{
		ID:               uuid.New(),
		Sku:              trimToNil(draft.Sku),
		Barcode:          strings.TrimSpace(draft.Barcode),
		Gtin:             trimToNil(draft.Gtin),
		Mpn:              trimToNil(draft.Mpn),
		AxisValueEntryID: draft.AxisValueEntryID,
		AxisValue:        trimToNil(draft.AxisValue),
		AttributeValues:  orEmptyAttr(draft.AttributeValues),
		VariantValues:    orEmptyVar(draft.VariantValues),
	})
}

// NewProduct, doğrulanmış yeni ürün oluşturur; her kalem için
// ProductItemCreated bütünleşme olayı biriktirir.
func NewProduct(
	groupID, categoryID uuid.UUID,
	modelCode, name string,
	status Status,
	attributeValues []AttributeValue,
	variants []VariantRef,
	items []ItemDraft,
	groupCode, slicerValue *string,
	brandID *uuid.UUID,
	description *string,
) sharedkernel.ResultOf[*Product] {
	if groupID == uuid.Nil {
		return sharedkernel.FailOf[*Product](sharedkernel.NewValidationError("Group id is required."))
	}
	if categoryID == uuid.Nil {
		return sharedkernel.FailOf[*Product](sharedkernel.NewValidationError("Category id is required."))
	}
	if strings.TrimSpace(name) == "" {
		return sharedkernel.FailOf[*Product](sharedkernel.NewValidationError("Product name is required."))
	}
	if strings.TrimSpace(modelCode) == "" {
		return sharedkernel.FailOf[*Product](sharedkernel.NewValidationError("Model code is required."))
	}
	if structure := ValidateVariantStructure(len(variants), len(items)); structure.IsFailure() {
		return sharedkernel.FailOf[*Product](structure.Err())
	}

	product := &Product{
		ID:              uuid.New(),
		GroupID:         groupID,
		CategoryID:      categoryID,
		BrandID:         brandID,
		ModelCode:       strings.TrimSpace(modelCode),
		GroupCode:       trimToNil(groupCode),
		SlicerValue:     trimToNil(slicerValue),
		Name:            strings.TrimSpace(name),
		Description:     trimToNil(description),
		Status:          status,
		AttributeValues: orEmptyAttr(attributeValues),
		Variants:        variants,
	}
	for _, draft := range items {
		itemResult := newItem(draft)
		if itemResult.IsFailure() {
			return sharedkernel.FailOf[*Product](itemResult.Err())
		}
		item := itemResult.Value()
		product.Items = append(product.Items, item)
		product.raise(outbox.ProductItemCreated{
			ProductItemID: item.ID, ProductID: product.ID, OccurredOnUtc: time.Now().UTC()})
	}
	return sharedkernel.OkOf(product)
}

// UpdateDetails, ürün ad/durum/kategori/marka/açıklama/KDV oranını ve
// (verilmişse) özellik değerlerini günceller; içerik değişikliği olayı yayar.
func (p *Product) UpdateDetails(categoryID uuid.UUID, name string, status Status, attributeValues []AttributeValue, brandID *uuid.UUID, description, vatRate *string) sharedkernel.Result {
	if categoryID == uuid.Nil {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Category id is required."))
	}
	if strings.TrimSpace(name) == "" {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Product name is required."))
	}
	p.CategoryID = categoryID
	p.Name = strings.TrimSpace(name)
	p.Status = status
	p.BrandID = brandID
	p.Description = trimToNil(description)
	p.VatRate = trimToNil(vatRate)
	if attributeValues != nil {
		p.AttributeValues = attributeValues
	}
	p.raiseContentChanged()
	return sharedkernel.Ok()
}

// raiseContentChanged, pazaryerine giden içeriğin değiştiğini duyurur; kalemi
// olmayan ürün için olay yayımlanmaz (listeleme kalem düzeyindedir).
func (p *Product) raiseContentChanged() {
	if len(p.Items) == 0 {
		return
	}
	ids := make([]uuid.UUID, len(p.Items))
	for i, item := range p.Items {
		ids[i] = item.ID
	}
	p.raise(outbox.ProductContentChanged{ProductID: p.ID, ProductItemIds: ids, OccurredOnUtc: time.Now().UTC()})
}

// UpdateItem, belirtilen kalemin bilgilerini günceller; barkod/SKU ürün içinde
// benzersiz kalmalıdır. Başarıda yalnızca o kalem için içerik olayı yayar.
func (p *Product) UpdateItem(itemID uuid.UUID, update ItemUpdate) sharedkernel.Result {
	item := p.findItem(itemID)
	if item == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Product variant not found."))
	}

	if update.Barcode != nil && strings.TrimSpace(*update.Barcode) != "" {
		trimmed := strings.TrimSpace(*update.Barcode)
		for _, other := range p.Items {
			if other.ID != itemID && strings.EqualFold(other.Barcode, trimmed) {
				return sharedkernel.Fail(sharedkernel.NewConflictError("Barcode already exists on this product."))
			}
		}
	}
	if update.Sku != nil && strings.TrimSpace(*update.Sku) != "" {
		trimmed := strings.TrimSpace(*update.Sku)
		for _, other := range p.Items {
			if other.ID != itemID && other.Sku != nil && strings.EqualFold(*other.Sku, trimmed) {
				return sharedkernel.Fail(sharedkernel.NewConflictError("Variant SKU already exists on this product."))
			}
		}
	}

	if update.Barcode != nil {
		if strings.TrimSpace(*update.Barcode) == "" {
			return sharedkernel.Fail(sharedkernel.NewValidationError("Variant barcode is required."))
		}
		item.Barcode = strings.TrimSpace(*update.Barcode)
	}
	if update.Sku != nil {
		item.Sku = trimToNil(update.Sku)
	}
	item.Gtin = trimToNil(update.Gtin)
	item.Mpn = trimToNil(update.Mpn)
	item.AxisValueEntryID = update.AxisValueEntryID
	item.AxisValue = trimToNil(update.AxisValue)
	if update.AttributeValues != nil {
		item.AttributeValues = update.AttributeValues
	}

	p.raise(outbox.ProductContentChanged{
		ProductID: p.ID, ProductItemIds: []uuid.UUID{item.ID}, OccurredOnUtc: time.Now().UTC()})
	return sharedkernel.Ok()
}

// AddItem, ürüne yeni kalem ekler: kalem her eksen için tam bir seçim içermeli,
// eksen kombinasyonu/barkod/SKU ürün içinde benzersiz olmalıdır.
func (p *Product) AddItem(draft ItemDraft) sharedkernel.ResultOf[*ProductItem] {
	if len(p.Variants) == 0 {
		return sharedkernel.FailOf[*ProductItem](
			sharedkernel.NewValidationError("Basic product must have exactly one variant."))
	}

	selections := draft.VariantValues
	expected := map[uuid.UUID]struct{}{}
	for _, v := range p.Variants {
		expected[v.ID] = struct{}{}
	}
	selected := map[uuid.UUID]struct{}{}
	for _, s := range selections {
		selected[s.Variant.ID] = struct{}{}
	}
	if len(selections) != len(p.Variants) || !sameIDSet(expected, selected) {
		return sharedkernel.FailOf[*ProductItem](
			sharedkernel.NewValidationError("Item must include exactly one selection for each variant type."))
	}

	newKey := selectionKey(selections)
	for _, existing := range p.Items {
		if equalKeys(selectionKey(existing.VariantValues), newKey) {
			return sharedkernel.FailOf[*ProductItem](
				sharedkernel.NewConflictError("An item with the same variant selections already exists."))
		}
	}

	barcode := strings.TrimSpace(draft.Barcode)
	if barcode != "" {
		for _, existing := range p.Items {
			if strings.EqualFold(existing.Barcode, barcode) {
				return sharedkernel.FailOf[*ProductItem](sharedkernel.NewConflictError("Barcode already exists on this product."))
			}
		}
	}
	if draft.Sku != nil && strings.TrimSpace(*draft.Sku) != "" {
		trimmed := strings.TrimSpace(*draft.Sku)
		for _, existing := range p.Items {
			if existing.Sku != nil && strings.EqualFold(*existing.Sku, trimmed) {
				return sharedkernel.FailOf[*ProductItem](sharedkernel.NewConflictError("Variant SKU already exists on this product."))
			}
		}
	}

	itemResult := newItem(draft)
	if itemResult.IsFailure() {
		return itemResult
	}
	item := itemResult.Value()
	p.Items = append(p.Items, item)
	p.raise(outbox.ProductItemCreated{ProductItemID: item.ID, ProductID: p.ID, OccurredOnUtc: time.Now().UTC()})
	return sharedkernel.OkOf(item)
}

// RemoveItem, üründen bir kalemi kaldırır; en az bir kalem kalmalıdır.
func (p *Product) RemoveItem(itemID uuid.UUID) sharedkernel.Result {
	item := p.findItem(itemID)
	if item == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Product variant not found."))
	}
	if len(p.Variants) == 0 && len(p.Items) <= 1 {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Basic product must have exactly one variant."))
	}
	if len(p.Items) <= 1 {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Product must have at least one variant."))
	}
	for i, existing := range p.Items {
		if existing.ID == itemID {
			p.Items = append(p.Items[:i], p.Items[i+1:]...)
			break
		}
	}
	p.raise(outbox.ProductItemDeleted{ProductItemID: itemID, ProductID: p.ID, OccurredOnUtc: time.Now().UTC()})
	return sharedkernel.Ok()
}

// PrepareForRemoval, ürün silinmeden önce her kalem için ProductItemDeleted
// yayar; uydu context'ler fiyat/stok kayıtlarını böyle temizler.
func (p *Product) PrepareForRemoval() {
	for _, item := range p.Items {
		p.raise(outbox.ProductItemDeleted{ProductItemID: item.ID, ProductID: p.ID, OccurredOnUtc: time.Now().UTC()})
	}
}

// AddImage, galeriye görsel ekler; birincil seçilirse diğer birincil işaretler kaldırılır.
func (p *Product) AddImage(url string, sortOrder int, altText *string, isPrimary bool, variantValueID *uuid.UUID) sharedkernel.ResultOf[*ProductImage] {
	if strings.TrimSpace(url) == "" {
		return sharedkernel.FailOf[*ProductImage](sharedkernel.NewValidationError("Image URL is required."))
	}
	if isPrimary {
		p.clearPrimaryImage()
	}
	image := &ProductImage{
		ID:             uuid.New(),
		URL:            strings.TrimSpace(url),
		SortOrder:      sortOrder,
		AltText:        trimToNil(altText),
		IsPrimary:      isPrimary,
		VariantValueID: variantValueID,
	}
	p.Images = append(p.Images, image)
	p.raiseContentChanged()
	return sharedkernel.OkOf(image)
}

// UpdateImage, mevcut galeri görselini günceller.
func (p *Product) UpdateImage(imageID uuid.UUID, url string, sortOrder int, altText *string, isPrimary bool, variantValueID *uuid.UUID) sharedkernel.Result {
	image := p.findImage(imageID)
	if image == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Product image not found."))
	}
	if strings.TrimSpace(url) == "" {
		return sharedkernel.Fail(sharedkernel.NewValidationError("Image URL is required."))
	}
	if isPrimary {
		for _, other := range p.Images {
			if other.ID != imageID && other.IsPrimary {
				other.IsPrimary = false
			}
		}
	}
	image.URL = strings.TrimSpace(url)
	image.SortOrder = sortOrder
	image.AltText = trimToNil(altText)
	image.IsPrimary = isPrimary
	image.VariantValueID = variantValueID
	p.raiseContentChanged()
	return sharedkernel.Ok()
}

// RemoveImage, galeriden bir görseli kaldırır.
func (p *Product) RemoveImage(imageID uuid.UUID) sharedkernel.Result {
	for i, image := range p.Images {
		if image.ID == imageID {
			p.Images = append(p.Images[:i], p.Images[i+1:]...)
			p.raiseContentChanged()
			return sharedkernel.Ok()
		}
	}
	return sharedkernel.Fail(sharedkernel.NewNotFoundError("Product image not found."))
}

// clearPrimaryImage, tüm birincil işaretleri kaldırır.
func (p *Product) clearPrimaryImage() {
	for _, image := range p.Images {
		image.IsPrimary = false
	}
}

// findItem, kimlikle kalemi döner; yoksa nil.
func (p *Product) findItem(id uuid.UUID) *ProductItem {
	for _, item := range p.Items {
		if item.ID == id {
			return item
		}
	}
	return nil
}

// findImage, kimlikle görseli döner; yoksa nil.
func (p *Product) findImage(id uuid.UUID) *ProductImage {
	for _, image := range p.Images {
		if image.ID == id {
			return image
		}
	}
	return nil
}

// selectionKey, eksen seçimlerini tür kimliğine göre sıralayıp değer kimlik
// listesi olarak döner; kombinasyon eşitliği bu anahtarla karşılaştırılır.
func selectionKey(selections []VariantValue) []uuid.UUID {
	sorted := make([]VariantValue, len(selections))
	copy(sorted, selections)
	for i := 1; i < len(sorted); i++ {
		for j := i; j > 0 && sorted[j-1].Variant.ID.String() > sorted[j].Variant.ID.String(); j-- {
			sorted[j-1], sorted[j] = sorted[j], sorted[j-1]
		}
	}
	key := make([]uuid.UUID, len(sorted))
	for i, s := range sorted {
		key[i] = s.ID
	}
	return key
}

// equalKeys, iki seçim anahtarının eşitliğini döner.
func equalKeys(a, b []uuid.UUID) bool {
	if len(a) != len(b) {
		return false
	}
	for i := range a {
		if a[i] != b[i] {
			return false
		}
	}
	return true
}

// sameIDSet, iki kimlik kümesinin eşitliğini döner.
func sameIDSet(a, b map[uuid.UUID]struct{}) bool {
	if len(a) != len(b) {
		return false
	}
	for id := range a {
		if _, ok := b[id]; !ok {
			return false
		}
	}
	return true
}

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

// orEmptyAttr, nil listeyi boş dilime çevirir (jsonb'de [] yazılması için).
func orEmptyAttr(values []AttributeValue) []AttributeValue {
	if values == nil {
		return []AttributeValue{}
	}
	return values
}

// orEmptyVar, nil listeyi boş dilime çevirir.
func orEmptyVar(values []VariantValue) []VariantValue {
	if values == nil {
		return []VariantValue{}
	}
	return values
}
