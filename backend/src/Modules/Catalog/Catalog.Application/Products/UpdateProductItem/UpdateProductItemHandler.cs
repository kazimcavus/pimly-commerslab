using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Products;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.UpdateProductItem;

/// <summary>Ürün varyantı güncelleme işlemini yürüten handler.</summary>
public sealed class UpdateProductItemHandler(
    IValidator<UpdateProductItemCommand> validator,
    IProductRepository products,
    IAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IUpdateProductItemHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductItemDto>> ExecuteAsync(
        UpdateProductItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductItemDto>(validationResult.Error);
        }

        var product = await products.GetByItemIdAsync(command.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductItemDto>(Error.NotFound("Product variant not found."));
        }

        var current = product.Items.First(item => item.Id == command.Id);

        // Barkod/SKU değişiyorsa tenant genelinde benzersiz kalmalı (ürün içi kontrol domain'de).
        var newBarcode = command.Barcode?.Trim();
        if (!string.IsNullOrWhiteSpace(newBarcode)
            && !string.Equals(newBarcode, current.Barcode, StringComparison.OrdinalIgnoreCase)
            && await products.BarcodeExistsAsync(newBarcode, cancellationToken))
        {
            return Result.Failure<ProductItemDto>(Error.Conflict("Barcode is already in use."));
        }

        var newSku = command.Sku?.Trim();
        if (!string.IsNullOrWhiteSpace(newSku)
            && !string.Equals(newSku, current.Sku, StringComparison.OrdinalIgnoreCase)
            && await products.VariantSkuExistsAsync(newSku, cancellationToken))
        {
            return Result.Failure<ProductItemDto>(Error.Conflict("Variant SKU is already in use."));
        }

        var attributeValuesResult = await ProductCreationSupport.ResolveAttributeValuesAsync(
            attributes,
            command.AttributeValueInputs,
            cancellationToken);

        if (attributeValuesResult.IsFailure)
        {
            return Result.Failure<ProductItemDto>(attributeValuesResult.Error);
        }

        var update = new ProductItemUpdate(
            command.Gtin,
            command.Mpn,
            command.AxisValueEntryId,
            command.AxisValue,
            command.Stock,
            command.AttributeValueInputs is null ? null : attributeValuesResult.Value,
            command.Sku,
            command.Barcode);

        var updateResult = product.UpdateItem(command.Id, update);
        if (updateResult.IsFailure)
        {
            return Result.Failure<ProductItemDto>(updateResult.Error);
        }

        products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var variant = product.Items.First(v => v.Id == command.Id);
        return Result.Success(variant.ToDto(product.Id));
    }
}
