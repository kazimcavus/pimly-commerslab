package application

import (
	"context"
	"fmt"
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/attributes"
	"pimly.commerslab/backend-go/internal/modules/catalog/domain/categories"
	"pimly.commerslab/backend-go/internal/modules/catalog/domain/products"
	"pimly.commerslab/backend-go/internal/modules/catalog/domain/variants"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Ürün alanı uzunluk sınırları (.NET CatalogValidationRules sabitleri).
const (
	ModelCodeMaxLength      = 200
	ProductNameMaxLength    = 500
	VariantBarcodeMaxLength = 200
	VariantSkuMaxLength     = 200
)

// ProductAttributeValueDto, özellik değeri anlık görüntüsünün kablo biçimidir.
type ProductAttributeValueDto struct {
	Attribute AttributeDto `json:"attribute"`
	ID        uuid.UUID    `json:"id"`
	Name      string       `json:"name"`
}

// ProductVariantDto, üründe sabitlenen eksen tanımının kablo biçimidir.
type ProductVariantDto struct {
	ID             uuid.UUID `json:"id"`
	Name           string    `json:"name"`
	SelectionStyle string    `json:"selection_style"`
	Slicer         bool      `json:"slicer"`
}

// ProductVariantValueDto, eksen değeri anlık görüntüsünün kablo biçimidir.
type ProductVariantValueDto struct {
	Variant ProductVariantDto `json:"variant"`
	ID      uuid.UUID         `json:"id"`
	Name    string            `json:"name"`
}

// ProductItemDto, ürün kalemi kablo biçimidir.
type ProductItemDto struct {
	ID               uuid.UUID                  `json:"id"`
	ProductID        uuid.UUID                  `json:"product_id"`
	Sku              *string                    `json:"sku"`
	Barcode          string                     `json:"barcode"`
	Gtin             *string                    `json:"gtin"`
	Mpn              *string                    `json:"mpn"`
	AxisValueEntryID *uuid.UUID                 `json:"axis_value_entry_id"`
	AxisValue        *string                    `json:"axis_value"`
	AttributeValues  []ProductAttributeValueDto `json:"attribute_values"`
	VariantValues    []ProductVariantValueDto   `json:"variant_values"`
}

// ProductImageDto, galeri görseli kablo biçimidir.
type ProductImageDto struct {
	ID             uuid.UUID  `json:"id"`
	URL            string     `json:"url"`
	SortOrder      int        `json:"sort_order"`
	AltText        *string    `json:"alt_text"`
	IsPrimary      bool       `json:"is_primary"`
	VariantValueID *uuid.UUID `json:"variant_value_id"`
}

// ProductDto, ürün API yanıt modelidir. GroupCode grubun paylaşılan kodu
// (pazaryeri "model kodu"), SlicerValue bölünen eksen değeridir.
type ProductDto struct {
	ID              uuid.UUID                  `json:"id"`
	GroupID         uuid.UUID                  `json:"group_id"`
	CategoryID      uuid.UUID                  `json:"category_id"`
	ModelCode       string                     `json:"model_code"`
	Name            string                     `json:"name"`
	Status          string                     `json:"status"`
	AttributeValues []ProductAttributeValueDto `json:"attribute_values"`
	Variants        []ProductVariantDto        `json:"variants"`
	Items           []ProductItemDto           `json:"items"`
	Images          []ProductImageDto          `json:"images"`
	GroupCode       *string                    `json:"group_code"`
	SlicerValue     *string                    `json:"slicer_value"`
	BrandID         *uuid.UUID                 `json:"brand_id"`
	BrandName       *string                    `json:"brand_name"`
	Description     *string                    `json:"description"`
}

// CreateProductsBatchResultDto, toplu oluşturma yanıtıdır.
type CreateProductsBatchResultDto struct {
	Products []ProductDto `json:"products"`
}

// --- domain → DTO dönüşümleri (.NET ProductMappings karşılığı) ---

func attrValueToDto(v products.AttributeValue) ProductAttributeValueDto {
	return ProductAttributeValueDto{
		Attribute: AttributeDto{ID: v.Attribute.ID, Key: v.Attribute.Key, Name: v.Attribute.Name},
		ID:        v.ID, Name: v.Name,
	}
}

func productVariantToDto(v products.VariantRef) ProductVariantDto {
	return ProductVariantDto{ID: v.ID, Name: v.Name, SelectionStyle: v.SelectionStyle, Slicer: v.Slicer}
}

func productItemToDto(item *products.ProductItem, productID uuid.UUID) ProductItemDto {
	attrValues := make([]ProductAttributeValueDto, len(item.AttributeValues))
	for i, v := range item.AttributeValues {
		attrValues[i] = attrValueToDto(v)
	}
	varValues := make([]ProductVariantValueDto, len(item.VariantValues))
	for i, v := range item.VariantValues {
		varValues[i] = ProductVariantValueDto{Variant: productVariantToDto(v.Variant), ID: v.ID, Name: v.Name}
	}
	return ProductItemDto{
		ID: item.ID, ProductID: productID, Sku: item.Sku, Barcode: item.Barcode,
		Gtin: item.Gtin, Mpn: item.Mpn, AxisValueEntryID: item.AxisValueEntryID,
		AxisValue: item.AxisValue, AttributeValues: attrValues, VariantValues: varValues,
	}
}

func productImageToDto(image *products.ProductImage) ProductImageDto {
	return ProductImageDto{
		ID: image.ID, URL: image.URL, SortOrder: image.SortOrder,
		AltText: image.AltText, IsPrimary: image.IsPrimary, VariantValueID: image.VariantValueID,
	}
}

func productToDto(p *products.Product, brandName *string) ProductDto {
	attrValues := make([]ProductAttributeValueDto, len(p.AttributeValues))
	for i, v := range p.AttributeValues {
		attrValues[i] = attrValueToDto(v)
	}
	variantDtos := make([]ProductVariantDto, len(p.Variants))
	for i, v := range p.Variants {
		variantDtos[i] = productVariantToDto(v)
	}
	items := make([]ProductItemDto, len(p.Items))
	for i, item := range p.Items {
		items[i] = productItemToDto(item, p.ID)
	}
	images := make([]ProductImageDto, len(p.Images))
	for i, image := range p.Images {
		images[i] = productImageToDto(image)
	}
	return ProductDto{
		ID: p.ID, GroupID: p.GroupID, CategoryID: p.CategoryID, ModelCode: p.ModelCode,
		Name: p.Name, Status: string(p.Status), AttributeValues: attrValues,
		Variants: variantDtos, Items: items, Images: images,
		GroupCode: p.GroupCode, SlicerValue: p.SlicerValue, BrandID: p.BrandID,
		BrandName: brandName, Description: p.Description,
	}
}

// ProductRepository, ürün kalıcılık portudur (.NET IProductRepository karşılığı).
// Tüm metodlar tenant kimliğini açıkça alır; Add/Update/Remove aggregate'in
// biriktirdiği bütünleşme olaylarını AYNI transaction'da outbox'a yazar.
type ProductRepository interface {
	// GetByID, ürünü kalemleri ve görselleriyle döner; yoksa nil.
	GetByID(ctx context.Context, tenantID, id uuid.UUID) (*products.Product, error)

	// List, ürünleri grup + ada göre sıralı ve sayfalanmış listeler.
	List(ctx context.Context, tenantID uuid.UUID, p sharedkernel.Pagination) (sharedkernel.PagedResult[*products.Product], error)

	// GetByItemID, kalem kimliğinin ait olduğu ürünü döner; yoksa nil.
	GetByItemID(ctx context.Context, tenantID, itemID uuid.UUID) (*products.Product, error)

	// GetByImageID, görsel kimliğinin ait olduğu ürünü döner; yoksa nil.
	GetByImageID(ctx context.Context, tenantID, imageID uuid.UUID) (*products.Product, error)

	// ModelCodeExists, model kodunun tenant genelinde kullanımda olup olmadığını döner.
	ModelCodeExists(ctx context.Context, tenantID uuid.UUID, modelCode string) (bool, error)

	// BarcodeExists, barkodun tenant genelinde kullanımda olup olmadığını döner.
	BarcodeExists(ctx context.Context, tenantID uuid.UUID, barcode string) (bool, error)

	// VariantSkuExists, kalem SKU'sunun tenant genelinde kullanımda olup olmadığını döner.
	VariantSkuExists(ctx context.Context, tenantID uuid.UUID, sku string) (bool, error)

	// AddAll, ürünleri (kalem/görselleriyle) ve bekleyen olaylarını tek
	// transaction'da ekler (.NET'teki AddAsync döngüsü + SaveChanges).
	AddAll(ctx context.Context, tenantID uuid.UUID, items []*products.Product) error

	// Update, ürünün alanlarını, kalem/görsel koleksiyonlarını ve bekleyen
	// olaylarını tek transaction'da kalıcılaştırır.
	Update(ctx context.Context, tenantID uuid.UUID, product *products.Product) error

	// Remove, ürünü ve bekleyen olaylarını tek transaction'da siler
	// (kalem/görseller veritabanında cascade silinir).
	Remove(ctx context.Context, tenantID uuid.UUID, product *products.Product) error
}

// SkuPlanBuilder, ürün oluşturma planlarını üreten porttur
// (.NET ISkuGeneratorService.BuildPlansAsync karşılığı).
type SkuPlanBuilder interface {
	// BuildPlans, model kodu/generator yapılandırmasına göre bir veya birden
	// fazla oluşturma planı üretir (slicer bölmesi dahil).
	BuildPlans(ctx context.Context, tenantID uuid.UUID, modelCode string, codeInputs []string,
		name string, variantRefs []products.VariantRef, drafts []products.ItemDraft,
		splitOverrides []products.SplitOverride) sharedkernel.ResultOf[[]products.CreatePlan]
}

// --- komutlar ---

// AttributeValueInput, isteklerdeki özellik değeri girdisidir.
type AttributeValueInput struct {
	AttributeID      uuid.UUID `json:"attribute_id"`
	AttributeValueID uuid.UUID `json:"attribute_value_id"`
}

// VariantValueInput, isteklerdeki eksen değeri girdisidir.
type VariantValueInput struct {
	VariantID      uuid.UUID `json:"variant_id"`
	VariantValueID uuid.UUID `json:"variant_value_id"`
}

// VariantRefInput, isteklerdeki eksen tanımı girdisidir (yalnızca kimlik
// önemlidir; ad/stil katalogdan çözülür).
type VariantRefInput struct {
	ID uuid.UUID `json:"id"`
}

// CreateProductItemInput, ürün kalemi oluşturma girdisidir.
type CreateProductItemInput struct {
	Sku              *string
	Barcode          string
	Gtin             *string
	Mpn              *string
	AxisValueEntryID *uuid.UUID
	AxisValue        *string
	AttributeValues  []AttributeValueInput
	VariantValues    []VariantValueInput
}

// CreateProductCommand, tek ürün oluşturma komutudur.
type CreateProductCommand struct {
	GroupID     uuid.UUID
	CategoryID  uuid.UUID
	ModelCode   string
	Name        string
	Status      string
	CodeInputs  []string
	Attributes  []AttributeValueInput
	Variants    []VariantRefInput
	Items       []CreateProductItemInput
	BrandID     *uuid.UUID
	Description *string
}

// BatchSplitInput, tek slicer değerine (ör. "Antrasit") özgü geçersiz kılmaları taşır.
type BatchSplitInput struct {
	ValueName       string
	ModelCode       *string
	Name            *string
	Description     *string
	AttributeValues []AttributeValueInput
}

// CreateProductsBatchItem, toplu oluşturma girdisindeki tek ürün tanımıdır.
type CreateProductsBatchItem struct {
	CategoryID  uuid.UUID
	ModelCode   string
	Name        string
	Status      string
	CodeInputs  []string
	Attributes  []AttributeValueInput
	Variants    []VariantRefInput
	Items       []CreateProductItemInput
	Splits      []BatchSplitInput
	BrandID     *uuid.UUID
	Description *string
}

// CreateProductsBatchCommand, toplu ürün oluşturma komutudur.
// EnforceRequiredAttributes=false pazaryeri import'u için zorunlu-özellik
// doğrulamasını atlar.
type CreateProductsBatchCommand struct {
	GroupID                   uuid.UUID
	Products                  []CreateProductsBatchItem
	EnforceRequiredAttributes bool
}

// UpdateProductCommand, ürün güncelleme komutudur; Attributes nil ise mevcut
// değerler korunur.
type UpdateProductCommand struct {
	ID          uuid.UUID
	CategoryID  uuid.UUID
	Name        string
	Status      string
	Attributes  []AttributeValueInput // nil = koru
	BrandID     *uuid.UUID
	Description *string
}

// --- doğrulama ---

// isNumericBarcode, barkodun yalnızca rakamlardan oluştuğunu denetler.
func isNumericBarcode(value string) bool {
	if value == "" {
		return false
	}
	for _, ch := range value {
		if ch < '0' || ch > '9' {
			return false
		}
	}
	return true
}

// validateProductStatus, durum alanı kurallarını uygular (.NET ProductStatus kuralı).
func validateProductStatus(f *fieldErrors, field, value string) {
	if value == "" {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: field, Code: sharedkernel.ValidationCodeRequired, Message: "Status is required."})
		return
	}
	if _, ok := products.ParseStatus(value); !ok {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: field, Code: sharedkernel.ValidationCodeInvalidEnum, Message: "Status has an invalid value."})
	}
}

// validateItemInputs, kalem girdilerinin barkod/SKU kurallarını uygular;
// alan adları FluentValidation'ın çocuk doğrulayıcı biçimini izler.
func validateItemInputs(f *fieldErrors, prefix string, items []CreateProductItemInput) {
	if len(items) == 0 {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: prefix, Code: sharedkernel.ValidationCodeRequired, Message: "Items is required."})
		return
	}
	for i, item := range items {
		barcodeField := fmt.Sprintf("%s[%d].barcode", prefix, i)
		trimmed := item.Barcode
		if trimmed == "" {
			f.errs = append(f.errs, sharedkernel.ValidationError{
				Field: barcodeField, Code: sharedkernel.ValidationCodeRequired, Message: "Barcode is required."})
		} else {
			f.maxLength(barcodeField, "Barcode", trimmed, VariantBarcodeMaxLength)
			if !isNumericBarcode(strings.TrimSpace(trimmed)) {
				f.errs = append(f.errs, sharedkernel.ValidationError{
					Field: barcodeField, Code: "PredicateValidator", Message: "Barcode must be numeric."})
			}
		}
		f.maxLength(fmt.Sprintf("%s[%d].sku", prefix, i), "Sku", deref(item.Sku), VariantSkuMaxLength)
	}
}

// ProductHandlers, ürün kullanım senaryolarını yürütür (.NET'teki ürün
// handler sınıflarının Go karşılığı).
type ProductHandlers struct {
	products         ProductRepository
	categories       CategoryRepository
	brands           BrandRepository
	variants         VariantRepository
	attributes       AttributeRepository
	planner          SkuPlanBuilder
	allowedURLPrefix string
}

// NewProductHandlers, bağımlılıklarıyla ürün handler'larını oluşturur;
// allowedURLPrefix, görsel URL denetimi için Media:AllowedUrlPrefix değeridir.
func NewProductHandlers(
	productRepo ProductRepository,
	categoryRepo CategoryRepository,
	brandRepo BrandRepository,
	variantRepo VariantRepository,
	attributeRepo AttributeRepository,
	planner SkuPlanBuilder,
	allowedURLPrefix string,
) *ProductHandlers {
	return &ProductHandlers{
		products: productRepo, categories: categoryRepo, brands: brandRepo,
		variants: variantRepo, attributes: attributeRepo, planner: planner,
		allowedURLPrefix: allowedURLPrefix,
	}
}

// --- çözümleme yardımcıları (.NET ProductCreationSupport karşılığı) ---

// resolveVariantRefs, girdideki eksen kimliklerini katalog anlık görüntülerine çözer.
func (h *ProductHandlers) resolveVariantRefs(ctx context.Context, tenantID uuid.UUID, inputs []VariantRefInput) sharedkernel.ResultOf[[]products.VariantRef] {
	resolved := []products.VariantRef{}
	for _, input := range inputs {
		variantType, err := h.variants.GetByID(ctx, tenantID, input.ID)
		if err != nil {
			return sharedkernel.FailOf[[]products.VariantRef](sharedkernel.NewInternalError(err.Error()))
		}
		if variantType == nil {
			return sharedkernel.FailOf[[]products.VariantRef](sharedkernel.NewNotFoundError(
				fmt.Sprintf("Variant type '%s' not found.", input.ID)))
		}
		resolved = append(resolved, products.VariantRef{
			ID: variantType.ID, Name: variantType.Name,
			SelectionStyle: string(variantType.SelectionStyle), Slicer: variantType.Slicer,
		})
	}
	return sharedkernel.OkOf(resolved)
}

// resolveVariantValues, girdideki eksen değeri seçimlerini anlık görüntülere çözer.
func (h *ProductHandlers) resolveVariantValues(ctx context.Context, tenantID uuid.UUID, inputs []VariantValueInput) sharedkernel.ResultOf[[]products.VariantValue] {
	resolved := []products.VariantValue{}
	for _, input := range inputs {
		variantType, err := h.variants.GetByID(ctx, tenantID, input.VariantID)
		if err != nil {
			return sharedkernel.FailOf[[]products.VariantValue](sharedkernel.NewInternalError(err.Error()))
		}
		if variantType == nil {
			return sharedkernel.FailOf[[]products.VariantValue](sharedkernel.NewNotFoundError(
				fmt.Sprintf("Variant type '%s' not found.", input.VariantID)))
		}
		var value *variants.Value
		for _, candidate := range variantType.Values {
			if candidate.ID == input.VariantValueID {
				value = candidate
				break
			}
		}
		if value == nil {
			return sharedkernel.FailOf[[]products.VariantValue](sharedkernel.NewNotFoundError(fmt.Sprintf(
				"Variant value '%s' not found for type '%s'.", input.VariantValueID, input.VariantID)))
		}
		key := value.Key
		resolved = append(resolved, products.VariantValue{
			Variant: products.VariantRef{
				ID: variantType.ID, Name: variantType.Name,
				SelectionStyle: string(variantType.SelectionStyle), Slicer: variantType.Slicer,
			},
			ID: value.ID, Name: value.Label, Key: &key,
		})
	}
	return sharedkernel.OkOf(resolved)
}

// resolveAttributeValues, girdideki özellik değeri seçimlerini anlık görüntülere çözer.
func (h *ProductHandlers) resolveAttributeValues(ctx context.Context, tenantID uuid.UUID, inputs []AttributeValueInput) sharedkernel.ResultOf[[]products.AttributeValue] {
	resolved := []products.AttributeValue{}
	for _, input := range inputs {
		attribute, err := h.attributes.GetByID(ctx, tenantID, input.AttributeID)
		if err != nil {
			return sharedkernel.FailOf[[]products.AttributeValue](sharedkernel.NewInternalError(err.Error()))
		}
		if attribute == nil {
			return sharedkernel.FailOf[[]products.AttributeValue](sharedkernel.NewNotFoundError(
				fmt.Sprintf("Attribute '%s' not found.", input.AttributeID)))
		}
		var value *attributes.Value
		for _, candidate := range attribute.Values {
			if candidate.ID == input.AttributeValueID {
				value = candidate
				break
			}
		}
		if value == nil {
			return sharedkernel.FailOf[[]products.AttributeValue](sharedkernel.NewNotFoundError(fmt.Sprintf(
				"Attribute value '%s' not found for attribute '%s'.", input.AttributeValueID, input.AttributeID)))
		}
		resolved = append(resolved, products.AttributeValue{
			Attribute: products.AttributeRef{ID: attribute.ID, Key: attribute.Key, Name: attribute.Name},
			ID:        value.ID, Name: value.Name,
		})
	}
	return sharedkernel.OkOf(resolved)
}

// resolveItemDrafts, kalem girdilerini taslaklara çözer.
func (h *ProductHandlers) resolveItemDrafts(ctx context.Context, tenantID uuid.UUID, items []CreateProductItemInput) sharedkernel.ResultOf[[]products.ItemDraft] {
	drafts := make([]products.ItemDraft, 0, len(items))
	for _, item := range items {
		variantValues := h.resolveVariantValues(ctx, tenantID, item.VariantValues)
		if variantValues.IsFailure() {
			return sharedkernel.FailOf[[]products.ItemDraft](variantValues.Err())
		}
		attributeValues := h.resolveAttributeValues(ctx, tenantID, item.AttributeValues)
		if attributeValues.IsFailure() {
			return sharedkernel.FailOf[[]products.ItemDraft](attributeValues.Err())
		}
		drafts = append(drafts, products.ItemDraft{
			Sku: item.Sku, Barcode: item.Barcode, Gtin: item.Gtin, Mpn: item.Mpn,
			AxisValueEntryID: item.AxisValueEntryID, AxisValue: item.AxisValue,
			AttributeValues: attributeValues.Value(), VariantValues: variantValues.Value(),
		})
	}
	return sharedkernel.OkOf(drafts)
}

// ensureRequiredCategoryAttributes, kategoride zorunlu model-düzeyi
// özniteliklerin girdide karşılığı olduğunu doğrular; slicer/kalem seviyeli
// atamalar kanal hazırlık kontrolüne bırakılır.
func (h *ProductHandlers) ensureRequiredCategoryAttributes(ctx context.Context, tenantID uuid.UUID, category *categories.Category, providedIDs map[uuid.UUID]struct{}) sharedkernel.Result {
	for _, assignment := range category.Assignments {
		if !assignment.Required || assignment.Scope != categories.ScopeModel {
			continue
		}
		if _, ok := providedIDs[assignment.AttributeID]; ok {
			continue
		}
		attribute, err := h.attributes.GetByID(ctx, tenantID, assignment.AttributeID)
		if err != nil {
			return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
		}
		name := assignment.AttributeID.String()
		if attribute != nil {
			name = attribute.Name
		}
		return sharedkernel.Fail(sharedkernel.NewValidationError("Required attribute missing: " + name))
	}
	return sharedkernel.Ok()
}

// ensurePlanIsUnique, planın model kodu/barkod/SKU değerlerinin tenant
// genelinde benzersiz olduğunu doğrular.
func (h *ProductHandlers) ensurePlanIsUnique(ctx context.Context, tenantID uuid.UUID, plan products.CreatePlan) sharedkernel.Result {
	exists, err := h.products.ModelCodeExists(ctx, tenantID, plan.ModelCode)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if exists {
		return sharedkernel.Fail(sharedkernel.NewConflictError(
			fmt.Sprintf("Model code '%s' already exists.", plan.ModelCode)))
	}
	for _, item := range plan.Items {
		exists, err := h.products.BarcodeExists(ctx, tenantID, item.Barcode)
		if err != nil {
			return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
		}
		if exists {
			return sharedkernel.Fail(sharedkernel.NewConflictError(
				fmt.Sprintf("Barcode '%s' already exists.", item.Barcode)))
		}
		if item.Sku != nil && strings.TrimSpace(*item.Sku) != "" {
			exists, err := h.products.VariantSkuExists(ctx, tenantID, *item.Sku)
			if err != nil {
				return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
			}
			if exists {
				return sharedkernel.Fail(sharedkernel.NewConflictError(
					fmt.Sprintf("Variant SKU '%s' already exists.", *item.Sku)))
			}
		}
	}
	return sharedkernel.Ok()
}

// providedAttributeIDs, girdi listesindeki öznitelik kimliklerini kümeye çevirir.
func providedAttributeIDs(inputs []AttributeValueInput) map[uuid.UUID]struct{} {
	out := map[uuid.UUID]struct{}{}
	for _, input := range inputs {
		out[input.AttributeID] = struct{}{}
	}
	return out
}

// List, ürünleri kalem ve görselleriyle sayfalanmış döner.
func (h *ProductHandlers) List(ctx context.Context, tenantID uuid.UUID, page, pageSize int) sharedkernel.ResultOf[sharedkernel.PagedResult[ProductDto]] {
	pr := sharedkernel.ResolvePagination(page, pageSize)
	if pr.IsFailure() {
		return sharedkernel.FailOf[sharedkernel.PagedResult[ProductDto]](pr.Err())
	}
	pageResult, err := h.products.List(ctx, tenantID, pr.Value())
	if err != nil {
		return sharedkernel.FailOf[sharedkernel.PagedResult[ProductDto]](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(sharedkernel.MapPagedResult(pageResult, func(p *products.Product) ProductDto {
		return productToDto(p, nil)
	}))
}

// Get, tek ürünü döner; yoksa not_found.
func (h *ProductHandlers) Get(ctx context.Context, tenantID, id uuid.UUID) sharedkernel.ResultOf[ProductDto] {
	product, err := h.products.GetByID(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.FailOf[ProductDto](sharedkernel.NewInternalError(err.Error()))
	}
	if product == nil {
		return sharedkernel.FailOf[ProductDto](sharedkernel.NewNotFoundError("Product not found."))
	}
	return sharedkernel.OkOf(productToDto(product, nil))
}

// Delete, ürünü siler; her kalem için silme olayı outbox'a düşer.
func (h *ProductHandlers) Delete(ctx context.Context, tenantID, id uuid.UUID) sharedkernel.Result {
	product, err := h.products.GetByID(ctx, tenantID, id)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if product == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Product not found."))
	}
	product.PrepareForRemoval()
	if err := h.products.Remove(ctx, tenantID, product); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}

// Update, ürün ayrıntılarını günceller (.NET UpdateProductHandler portu).
func (h *ProductHandlers) Update(ctx context.Context, tenantID uuid.UUID, cmd UpdateProductCommand) sharedkernel.ResultOf[ProductDto] {
	var f fieldErrors
	f.requiredID("id", "Id", cmd.ID)
	f.requiredID("category_id", "CategoryId", cmd.CategoryID)
	f.required("name", "Name", cmd.Name)
	f.maxLength("name", "Name", cmd.Name, ProductNameMaxLength)
	validateProductStatus(&f, "status", cmd.Status)
	if verr := f.failure(); verr != nil {
		return sharedkernel.FailOf[ProductDto](verr)
	}

	product, err := h.products.GetByID(ctx, tenantID, cmd.ID)
	if err != nil {
		return sharedkernel.FailOf[ProductDto](sharedkernel.NewInternalError(err.Error()))
	}
	if product == nil {
		return sharedkernel.FailOf[ProductDto](sharedkernel.NewNotFoundError("Product not found."))
	}

	category, err := h.categories.GetByID(ctx, tenantID, cmd.CategoryID)
	if err != nil {
		return sharedkernel.FailOf[ProductDto](sharedkernel.NewInternalError(err.Error()))
	}
	if category == nil {
		return sharedkernel.FailOf[ProductDto](sharedkernel.NewNotFoundError("Category not found."))
	}

	// Null girdi mevcut değerleri koruduğu için zorunluluk, güncelleme sonrası
	// geçerli olacak öznitelik kümesi üzerinden denetlenir.
	provided := map[uuid.UUID]struct{}{}
	if cmd.Attributes == nil {
		for _, value := range product.AttributeValues {
			provided[value.Attribute.ID] = struct{}{}
		}
	} else {
		provided = providedAttributeIDs(cmd.Attributes)
	}
	if required := h.ensureRequiredCategoryAttributes(ctx, tenantID, category, provided); required.IsFailure() {
		return sharedkernel.FailOf[ProductDto](required.Err())
	}

	var brandName *string
	if cmd.BrandID != nil {
		brand, err := h.brands.GetByID(ctx, tenantID, *cmd.BrandID)
		if err != nil {
			return sharedkernel.FailOf[ProductDto](sharedkernel.NewInternalError(err.Error()))
		}
		if brand == nil {
			return sharedkernel.FailOf[ProductDto](sharedkernel.NewNotFoundError("Brand not found."))
		}
		brandName = &brand.Name
	}

	var attributeValues []products.AttributeValue
	if cmd.Attributes != nil {
		resolved := h.resolveAttributeValues(ctx, tenantID, cmd.Attributes)
		if resolved.IsFailure() {
			return sharedkernel.FailOf[ProductDto](resolved.Err())
		}
		attributeValues = resolved.Value()
	}

	status, _ := products.ParseStatus(cmd.Status)
	if updateResult := product.UpdateDetails(cmd.CategoryID, cmd.Name, status, attributeValues, cmd.BrandID, cmd.Description); updateResult.IsFailure() {
		return sharedkernel.FailOf[ProductDto](updateResult.Err())
	}
	if err := h.products.Update(ctx, tenantID, product); err != nil {
		return sharedkernel.FailOf[ProductDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(productToDto(product, brandName))
}

// Create, tek ürün oluşturur (.NET CreateProductHandler portu). Slicer eksenli
// ürünler yalnızca toplu uçtan oluşturulabilir.
func (h *ProductHandlers) Create(ctx context.Context, tenantID uuid.UUID, cmd CreateProductCommand) sharedkernel.ResultOf[ProductDto] {
	var f fieldErrors
	if cmd.GroupID == uuid.Nil {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: "group_id", Code: sharedkernel.ValidationCodeInvalidID, Message: "GroupId must be a valid identifier."})
	}
	f.requiredID("category_id", "CategoryId", cmd.CategoryID)
	if strings.TrimSpace(cmd.ModelCode) != "" {
		f.maxLength("model_code", "ModelCode", cmd.ModelCode, ModelCodeMaxLength)
	}
	f.required("name", "Name", cmd.Name)
	f.maxLength("name", "Name", cmd.Name, ProductNameMaxLength)
	validateProductStatus(&f, "status", cmd.Status)
	validateItemInputs(&f, "items", cmd.Items)
	if verr := f.failure(); verr != nil {
		return sharedkernel.FailOf[ProductDto](verr)
	}

	category, err := h.categories.GetByID(ctx, tenantID, cmd.CategoryID)
	if err != nil {
		return sharedkernel.FailOf[ProductDto](sharedkernel.NewInternalError(err.Error()))
	}
	if category == nil {
		return sharedkernel.FailOf[ProductDto](sharedkernel.NewNotFoundError("Category not found."))
	}
	if required := h.ensureRequiredCategoryAttributes(ctx, tenantID, category, providedAttributeIDs(cmd.Attributes)); required.IsFailure() {
		return sharedkernel.FailOf[ProductDto](required.Err())
	}

	var brandName *string
	if cmd.BrandID != nil {
		brand, err := h.brands.GetByID(ctx, tenantID, *cmd.BrandID)
		if err != nil {
			return sharedkernel.FailOf[ProductDto](sharedkernel.NewInternalError(err.Error()))
		}
		if brand == nil {
			return sharedkernel.FailOf[ProductDto](sharedkernel.NewNotFoundError("Brand not found."))
		}
		brandName = &brand.Name
	}

	variantRefs := h.resolveVariantRefs(ctx, tenantID, cmd.Variants)
	if variantRefs.IsFailure() {
		return sharedkernel.FailOf[ProductDto](variantRefs.Err())
	}
	for _, ref := range variantRefs.Value() {
		if ref.Slicer {
			return sharedkernel.FailOf[ProductDto](sharedkernel.NewValidationError(
				"Products with a slicer variant type must be created using POST /products:batch."))
		}
	}

	attributeValues := h.resolveAttributeValues(ctx, tenantID, cmd.Attributes)
	if attributeValues.IsFailure() {
		return sharedkernel.FailOf[ProductDto](attributeValues.Err())
	}
	drafts := h.resolveItemDrafts(ctx, tenantID, cmd.Items)
	if drafts.IsFailure() {
		return sharedkernel.FailOf[ProductDto](drafts.Err())
	}

	plans := h.planner.BuildPlans(ctx, tenantID, cmd.ModelCode, cmd.CodeInputs, cmd.Name,
		variantRefs.Value(), drafts.Value(), nil)
	if plans.IsFailure() {
		return sharedkernel.FailOf[ProductDto](plans.Err())
	}
	plan := plans.Value()[0]

	if unique := h.ensurePlanIsUnique(ctx, tenantID, plan); unique.IsFailure() {
		return sharedkernel.FailOf[ProductDto](unique.Err())
	}

	status, _ := products.ParseStatus(cmd.Status)
	createResult := products.NewProduct(
		cmd.GroupID, cmd.CategoryID, plan.ModelCode, plan.Name, status,
		attributeValues.Value(), plan.Variants, plan.Items,
		plan.GroupCode, plan.SlicerValue, cmd.BrandID, cmd.Description)
	if createResult.IsFailure() {
		return sharedkernel.FailOf[ProductDto](createResult.Err())
	}
	product := createResult.Value()

	if err := h.products.AddAll(ctx, tenantID, []*products.Product{product}); err != nil {
		return sharedkernel.FailOf[ProductDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(productToDto(product, brandName))
}

// CreateBatch, toplu ürün oluşturur (.NET CreateProductsBatchHandler portu):
// her girdi planlara bölünür, parti içi ve kalıcı benzersizlik doğrulanır,
// tüm ürünler tek transaction'da yazılır.
func (h *ProductHandlers) CreateBatch(ctx context.Context, tenantID uuid.UUID, cmd CreateProductsBatchCommand) sharedkernel.ResultOf[CreateProductsBatchResultDto] {
	var f fieldErrors
	if cmd.GroupID == uuid.Nil {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: "group_id", Code: sharedkernel.ValidationCodeInvalidID, Message: "GroupId must be a valid identifier."})
	}
	if len(cmd.Products) == 0 {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: "products", Code: sharedkernel.ValidationCodeRequired, Message: "Products is required."})
	}
	for i, item := range cmd.Products {
		prefix := fmt.Sprintf("products[%d]", i)
		if strings.TrimSpace(item.ModelCode) != "" {
			f.maxLength(prefix+".model_code", "ModelCode", item.ModelCode, ModelCodeMaxLength)
		}
		f.requiredID(prefix+".category_id", "CategoryId", item.CategoryID)
		f.required(prefix+".name", "Name", item.Name)
		f.maxLength(prefix+".name", "Name", item.Name, ProductNameMaxLength)
		validateProductStatus(&f, prefix+".status", item.Status)
		validateItemInputs(&f, prefix+".items", item.Items)
	}
	if verr := f.failure(); verr != nil {
		return sharedkernel.FailOf[CreateProductsBatchResultDto](verr)
	}

	type planEntry struct {
		plan            products.CreatePlan
		categoryID      uuid.UUID
		status          products.Status
		attributeValues []products.AttributeValue
		brandID         *uuid.UUID
		description     *string
	}
	var planEntries []planEntry
	seenModelCodes := map[string]struct{}{}
	seenBarcodes := map[string]struct{}{}
	seenSkus := map[string]struct{}{}
	categoriesByID := map[uuid.UUID]*categories.Category{}

	for _, item := range cmd.Products {
		category, cached := categoriesByID[item.CategoryID]
		if !cached {
			loaded, err := h.categories.GetByID(ctx, tenantID, item.CategoryID)
			if err != nil {
				return sharedkernel.FailOf[CreateProductsBatchResultDto](sharedkernel.NewInternalError(err.Error()))
			}
			category = loaded
			categoriesByID[item.CategoryID] = loaded
		}
		if category == nil {
			return sharedkernel.FailOf[CreateProductsBatchResultDto](sharedkernel.NewNotFoundError("Category not found."))
		}

		if cmd.EnforceRequiredAttributes {
			if required := h.ensureRequiredCategoryAttributes(ctx, tenantID, category, providedAttributeIDs(item.Attributes)); required.IsFailure() {
				return sharedkernel.FailOf[CreateProductsBatchResultDto](required.Err())
			}
		}

		variantRefs := h.resolveVariantRefs(ctx, tenantID, item.Variants)
		if variantRefs.IsFailure() {
			return sharedkernel.FailOf[CreateProductsBatchResultDto](variantRefs.Err())
		}
		attributeValues := h.resolveAttributeValues(ctx, tenantID, item.Attributes)
		if attributeValues.IsFailure() {
			return sharedkernel.FailOf[CreateProductsBatchResultDto](attributeValues.Err())
		}
		drafts := h.resolveItemDrafts(ctx, tenantID, item.Items)
		if drafts.IsFailure() {
			return sharedkernel.FailOf[CreateProductsBatchResultDto](drafts.Err())
		}

		splitOverrides := make([]products.SplitOverride, 0, len(item.Splits))
		for _, split := range item.Splits {
			splitOverrides = append(splitOverrides, products.SplitOverride{
				ValueName: split.ValueName, ModelCode: split.ModelCode,
				Name: split.Name, Description: split.Description,
			})
		}

		plans := h.planner.BuildPlans(ctx, tenantID, item.ModelCode, item.CodeInputs, item.Name,
			variantRefs.Value(), drafts.Value(), splitOverrides)
		if plans.IsFailure() {
			return sharedkernel.FailOf[CreateProductsBatchResultDto](plans.Err())
		}

		// Slicer (renk) seviyeli özellik değerleri: değer adına göre çözülür ve
		// SlicerValue eşleşen plana model-düzeyi değerlerin üzerine eklenir.
		splitValuesByName := map[string][]products.AttributeValue{}
		for _, split := range item.Splits {
			if len(split.AttributeValues) == 0 {
				continue
			}
			resolved := h.resolveAttributeValues(ctx, tenantID, split.AttributeValues)
			if resolved.IsFailure() {
				return sharedkernel.FailOf[CreateProductsBatchResultDto](resolved.Err())
			}
			splitValuesByName[strings.ToLower(strings.TrimSpace(split.ValueName))] = resolved.Value()
		}

		for _, plan := range plans.Value() {
			if batch := ensureBatchUniqueness(plan, seenModelCodes, seenBarcodes, seenSkus); batch.IsFailure() {
				return sharedkernel.FailOf[CreateProductsBatchResultDto](batch.Err())
			}
			if unique := h.ensurePlanIsUnique(ctx, tenantID, plan); unique.IsFailure() {
				return sharedkernel.FailOf[CreateProductsBatchResultDto](unique.Err())
			}

			status, _ := products.ParseStatus(item.Status)
			description := plan.Description
			if description == nil {
				description = item.Description
			}
			planEntries = append(planEntries, planEntry{
				plan: plan, categoryID: item.CategoryID, status: status,
				attributeValues: mergeSplitAttributeValues(attributeValues.Value(), plan.SlicerValue, splitValuesByName),
				brandID:         item.BrandID, description: description,
			})
		}
	}

	created := make([]*products.Product, 0, len(planEntries))
	for _, entry := range planEntries {
		createResult := products.NewProduct(
			cmd.GroupID, entry.categoryID, entry.plan.ModelCode, entry.plan.Name, entry.status,
			entry.attributeValues, entry.plan.Variants, entry.plan.Items,
			entry.plan.GroupCode, entry.plan.SlicerValue, entry.brandID, entry.description)
		if createResult.IsFailure() {
			return sharedkernel.FailOf[CreateProductsBatchResultDto](createResult.Err())
		}
		created = append(created, createResult.Value())
	}

	if err := h.products.AddAll(ctx, tenantID, created); err != nil {
		return sharedkernel.FailOf[CreateProductsBatchResultDto](sharedkernel.NewInternalError(err.Error()))
	}

	dtos := make([]ProductDto, len(created))
	for i, product := range created {
		dtos[i] = productToDto(product, nil)
	}
	return sharedkernel.OkOf(CreateProductsBatchResultDto{Products: dtos})
}

// ensureBatchUniqueness, planın kodlarının parti içinde benzersizliğini doğrular.
func ensureBatchUniqueness(plan products.CreatePlan, modelCodes, barcodes, skus map[string]struct{}) sharedkernel.Result {
	codeKey := strings.ToLower(plan.ModelCode)
	if _, exists := modelCodes[codeKey]; exists {
		return sharedkernel.Fail(sharedkernel.NewConflictError(
			fmt.Sprintf("Duplicate model code '%s' in batch request.", plan.ModelCode)))
	}
	modelCodes[codeKey] = struct{}{}

	for _, item := range plan.Items {
		barcodeKey := strings.ToLower(item.Barcode)
		if _, exists := barcodes[barcodeKey]; exists {
			return sharedkernel.Fail(sharedkernel.NewConflictError(
				fmt.Sprintf("Duplicate barcode '%s' in batch request.", item.Barcode)))
		}
		barcodes[barcodeKey] = struct{}{}

		if item.Sku != nil && strings.TrimSpace(*item.Sku) != "" {
			skuKey := strings.ToLower(*item.Sku)
			if _, exists := skus[skuKey]; exists {
				return sharedkernel.Fail(sharedkernel.NewConflictError(
					fmt.Sprintf("Duplicate variant SKU '%s' in batch request.", *item.Sku)))
			}
			skus[skuKey] = struct{}{}
		}
	}
	return sharedkernel.Ok()
}

// mergeSplitAttributeValues, model-düzeyi değerlerin üzerine planın slicer
// değerine özgü değerleri ekler; aynı öznitelik iki düzeyde de varsa slicer
// değeri geçerlidir.
func mergeSplitAttributeValues(modelValues []products.AttributeValue, slicerValue *string, splitValuesByName map[string][]products.AttributeValue) []products.AttributeValue {
	if slicerValue == nil {
		return modelValues
	}
	splitValues, ok := splitValuesByName[strings.ToLower(strings.TrimSpace(*slicerValue))]
	if !ok {
		return modelValues
	}
	byAttributeID := map[uuid.UUID]products.AttributeValue{}
	var order []uuid.UUID
	for _, value := range modelValues {
		if _, exists := byAttributeID[value.Attribute.ID]; !exists {
			order = append(order, value.Attribute.ID)
		}
		byAttributeID[value.Attribute.ID] = value
	}
	for _, value := range splitValues {
		if _, exists := byAttributeID[value.Attribute.ID]; !exists {
			order = append(order, value.Attribute.ID)
		}
		byAttributeID[value.Attribute.ID] = value
	}
	merged := make([]products.AttributeValue, 0, len(order))
	for _, id := range order {
		merged = append(merged, byAttributeID[id])
	}
	return merged
}
