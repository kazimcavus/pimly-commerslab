package application

import (
	"context"
	"strings"

	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// ProductImageMaxLengths (.NET CatalogValidationRules sabitleri).
const (
	ProductImageUrlMaxLength     = 2000
	ProductImageAltTextMaxLength = 500
)

// ProductImageCommand, görsel ekleme/güncelleme komutudur.
type ProductImageCommand struct {
	ProductID      uuid.UUID // ekleme yolunda ürün kimliği
	ImageID        uuid.UUID // güncelleme/silme yolunda görsel kimliği
	URL            string
	SortOrder      int
	AltText        *string
	IsPrimary      bool
	VariantValueID *uuid.UUID
}

// validateProductImageInput, görsel URL/alt metin kurallarını uygular
// (.NET Add/UpdateProductImageCommandValidator portu): URL zorunludur ve bu
// tenant'ın medya deposuna işaret etmelidir.
func (h *ProductHandlers) validateProductImageInput(tenantID uuid.UUID, cmd ProductImageCommand) *sharedkernel.Error {
	var f fieldErrors
	f.required("url", "Url", cmd.URL)
	f.maxLength("url", "Url", cmd.URL, ProductImageUrlMaxLength)
	if strings.TrimSpace(cmd.URL) != "" && !isAllowedMediaURL(cmd.URL, h.allowedURLPrefix, tenantID) {
		f.errs = append(f.errs, sharedkernel.ValidationError{
			Field: "url", Code: sharedkernel.ValidationCodeInvalidFormat,
			Message: "Url must reference an uploaded media asset."})
	}
	f.maxLength("alt_text", "AltText", deref(cmd.AltText), ProductImageAltTextMaxLength)
	return f.failure()
}

// AddImage, ürün galerisine görsel ekler (.NET AddProductImageHandler portu).
func (h *ProductHandlers) AddImage(ctx context.Context, tenantID uuid.UUID, cmd ProductImageCommand) sharedkernel.ResultOf[ProductImageDto] {
	if verr := h.validateProductImageInput(tenantID, cmd); verr != nil {
		return sharedkernel.FailOf[ProductImageDto](verr)
	}

	product, err := h.products.GetByID(ctx, tenantID, cmd.ProductID)
	if err != nil {
		return sharedkernel.FailOf[ProductImageDto](sharedkernel.NewInternalError(err.Error()))
	}
	if product == nil {
		return sharedkernel.FailOf[ProductImageDto](sharedkernel.NewNotFoundError("Product not found."))
	}

	addResult := product.AddImage(cmd.URL, cmd.SortOrder, cmd.AltText, cmd.IsPrimary, cmd.VariantValueID)
	if addResult.IsFailure() {
		return sharedkernel.FailOf[ProductImageDto](addResult.Err())
	}
	if err := h.products.Update(ctx, tenantID, product); err != nil {
		return sharedkernel.FailOf[ProductImageDto](sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.OkOf(productImageToDto(addResult.Value()))
}

// UpdateImage, galeri görselini günceller (.NET UpdateProductImageHandler portu).
func (h *ProductHandlers) UpdateImage(ctx context.Context, tenantID uuid.UUID, cmd ProductImageCommand) sharedkernel.ResultOf[ProductImageDto] {
	if verr := h.validateProductImageInput(tenantID, cmd); verr != nil {
		return sharedkernel.FailOf[ProductImageDto](verr)
	}

	product, err := h.products.GetByImageID(ctx, tenantID, cmd.ImageID)
	if err != nil {
		return sharedkernel.FailOf[ProductImageDto](sharedkernel.NewInternalError(err.Error()))
	}
	if product == nil {
		return sharedkernel.FailOf[ProductImageDto](sharedkernel.NewNotFoundError("Product image not found."))
	}

	updateResult := product.UpdateImage(cmd.ImageID, cmd.URL, cmd.SortOrder, cmd.AltText, cmd.IsPrimary, cmd.VariantValueID)
	if updateResult.IsFailure() {
		return sharedkernel.FailOf[ProductImageDto](updateResult.Err())
	}
	if err := h.products.Update(ctx, tenantID, product); err != nil {
		return sharedkernel.FailOf[ProductImageDto](sharedkernel.NewInternalError(err.Error()))
	}
	for _, image := range product.Images {
		if image.ID == cmd.ImageID {
			return sharedkernel.OkOf(productImageToDto(image))
		}
	}
	return sharedkernel.FailOf[ProductImageDto](sharedkernel.NewNotFoundError("Product image not found."))
}

// RemoveImage, galeriden görsel kaldırır (.NET RemoveProductImageHandler portu).
func (h *ProductHandlers) RemoveImage(ctx context.Context, tenantID, imageID uuid.UUID) sharedkernel.Result {
	product, err := h.products.GetByImageID(ctx, tenantID, imageID)
	if err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	if product == nil {
		return sharedkernel.Fail(sharedkernel.NewNotFoundError("Product image not found."))
	}
	if removeResult := product.RemoveImage(imageID); removeResult.IsFailure() {
		return sharedkernel.Fail(removeResult.Err())
	}
	if err := h.products.Update(ctx, tenantID, product); err != nil {
		return sharedkernel.Fail(sharedkernel.NewInternalError(err.Error()))
	}
	return sharedkernel.Ok()
}
