package application

import (
	"context"
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/catalog/domain/products"
	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// AddProductItemCommand, mevcut ürüne kalem ekleme komutudur.
type AddProductItemCommand struct {
	ProductID uuid.UUID
	Item      CreateProductItemInput
}

// UpdateProductItemCommand, kalem güncelleme komutudur; Attributes nil ise
// mevcut değerler korunur, Barcode/Sku nil ise mevcut değer korunur.
type UpdateProductItemCommand struct {
	ID         uuid.UUID
	Gtin       *string
	Mpn        *string
	AxisValueEntryID *uuid.UUID
	AxisValue  *string
	Attributes []AttributeValueInput // nil = koru
	Sku        *string
	Barcode    *string
}

// GetItem, kalem kimliğiyle kalemi döner (.NET GetProductItemHandler portu).
func (h *ProductHandlers) GetItem(ctx context.Context, tenantID, itemID uuid.UUID) sharedkernel.ResultOf[ProductItemDto] {
	product, err := h.products.GetByItemID(ctx, tenantID, itemID)
	if err != nil {
		return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewInternalError(err.Error()))
	}
	if product == nil {
		return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewNotFoundError("Product variant not found."))
	}
	for _, item := range product.Items {
		if item.ID == itemID {
			return sharedkernel.OkOf(productItemToDto(item, product.ID))
		}
	}
	return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewNotFoundError("Product variant not found."))
}

// AddItem, mevcut ürüne yeni satılabilir kalem ekler (.NET AddProductItemHandler
// portu): barkod ve SKU tenant genelinde benzersiz olmalı, ürün içi denetimler
// domain'de yapılır.
func (h *ProductHandlers) AddItem(ctx context.Context, tenantID uuid.UUID, cmd AddProductItemCommand) sharedkernel.ResultOf[ProductItemDto] {
	var f fieldErrors
	f.requiredID("product_id", "Id", cmd.ProductID)
	if cmd.Item.Barcode == "" {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: "item.barcode", Code: sharedkernel.ValidationCodeUnknown, Message: "Barcode is required."})
	}
	if verr := f.failure(); verr != nil {
		return sharedkernel.FailOf[ProductItemDto](verr)
	}

	product, err := h.products.GetByID(ctx, tenantID, cmd.ProductID)
	if err != nil {
		return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewInternalError(err.Error()))
	}
	if product == nil {
		return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewNotFoundError("Product not found."))
	}

	exists, err := h.products.BarcodeExists(ctx, tenantID, strings.TrimSpace(cmd.Item.Barcode))
	if err != nil {
		return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewInternalError(err.Error()))
	}
	if exists {
		return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewConflictError("Barcode is already in use."))
	}
	if cmd.Item.Sku != nil && strings.TrimSpace(*cmd.Item.Sku) != "" {
		exists, err := h.products.VariantSkuExists(ctx, tenantID, strings.TrimSpace(*cmd.Item.Sku))
		if err != nil {
			return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewInternalError(err.Error()))
		}
		if exists {
			return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewConflictError("Variant SKU is already in use."))
		}
	}

	drafts := h.resolveItemDrafts(ctx, tenantID, []CreateProductItemInput{cmd.Item})
	if drafts.IsFailure() {
		return sharedkernel.FailOf[ProductItemDto](drafts.Err())
	}
	addResult := product.AddItem(drafts.Value()[0])
	if addResult.IsFailure() {
		return sharedkernel.FailOf[ProductItemDto](addResult.Err())
	}
	if err := h.products.Update(ctx, tenantID, product); err != nil {
		return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(productItemToDto(addResult.Value(), product.ID))
}

// UpdateItem, kalem bilgilerini günceller (.NET UpdateProductItemHandler portu).
func (h *ProductHandlers) UpdateItem(ctx context.Context, tenantID uuid.UUID, cmd UpdateProductItemCommand) sharedkernel.ResultOf[ProductItemDto] {
	product, err := h.products.GetByItemID(ctx, tenantID, cmd.ID)
	if err != nil {
		return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewInternalError(err.Error()))
	}
	if product == nil {
		return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewNotFoundError("Product variant not found."))
	}

	var current *products.ProductItem
	for _, item := range product.Items {
		if item.ID == cmd.ID {
			current = item
			break
		}
	}

	// Barkod/SKU değişiyorsa tenant genelinde benzersiz kalmalı (ürün içi
	// denetim domain'de yapılır).
	if cmd.Barcode != nil && strings.TrimSpace(*cmd.Barcode) != "" {
		trimmed := strings.TrimSpace(*cmd.Barcode)
		if !strings.EqualFold(trimmed, current.Barcode) {
			exists, err := h.products.BarcodeExists(ctx, tenantID, trimmed)
			if err != nil {
				return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewInternalError(err.Error()))
			}
			if exists {
				return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewConflictError("Barcode is already in use."))
			}
		}
	}
	if cmd.Sku != nil && strings.TrimSpace(*cmd.Sku) != "" {
		trimmed := strings.TrimSpace(*cmd.Sku)
		currentSku := ""
		if current.Sku != nil {
			currentSku = *current.Sku
		}
		if !strings.EqualFold(trimmed, currentSku) {
			exists, err := h.products.VariantSkuExists(ctx, tenantID, trimmed)
			if err != nil {
				return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewInternalError(err.Error()))
			}
			if exists {
				return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewConflictError("Variant SKU is already in use."))
			}
		}
	}

	var attributeValues []products.AttributeValue
	if cmd.Attributes != nil {
		resolved := h.resolveAttributeValues(ctx, tenantID, cmd.Attributes)
		if resolved.IsFailure() {
			return sharedkernel.FailOf[ProductItemDto](resolved.Err())
		}
		attributeValues = resolved.Value()
	}

	updateResult := product.UpdateItem(cmd.ID, products.ItemUpdate{
		Gtin: cmd.Gtin, Mpn: cmd.Mpn, AxisValueEntryID: cmd.AxisValueEntryID,
		AxisValue: cmd.AxisValue, AttributeValues: attributeValues,
		Sku: cmd.Sku, Barcode: cmd.Barcode,
	})
	if updateResult.IsFailure() {
		return sharedkernel.FailOf[ProductItemDto](updateResult.Err())
	}
	if err := h.products.Update(ctx, tenantID, product); err != nil {
		return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewInternalError(err.Error()))
	}
	for _, item := range product.Items {
		if item.ID == cmd.ID {
			return sharedkernel.OkOf(productItemToDto(item, product.ID))
		}
	}
	return sharedkernel.FailOf[ProductItemDto](sharedkernel.NewNotFoundError("Product variant not found."))
}

// DeleteItem, kalemi üründen kaldırır; silme olayı outbox'a düşer.
func (h *ProductHandlers) DeleteItem(ctx context.Context, tenantID, itemID uuid.UUID) sharedkernel.Result {
	product, err := h.products.GetByItemID(ctx, tenantID, itemID)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if product == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Product variant not found."))
	}
	if removeResult := product.RemoveItem(itemID); removeResult.IsFailure() {
		return sharedkernel.Fail(removeResult.Err())
	}
	if err := h.products.Update(ctx, tenantID, product); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}
