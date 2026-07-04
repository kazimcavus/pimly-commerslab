using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Products;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.AddProductItem;

/// <summary>Mevcut ürüne yeni satılabilir kalem ekleme işlemini yürüten handler.</summary>
public sealed class AddProductItemHandler(
    IValidator<AddProductItemCommand> validator,
    IProductRepository products,
    IVariantRepository variantTypes,
    IAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IAddProductItemHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductItemDto>> ExecuteAsync(
        AddProductItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductItemDto>(validationResult.Error);
        }

        var product = await products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductItemDto>(Error.NotFound("Product not found."));
        }

        // Barkod ve SKU tenant genelinde benzersizdir; ürün içi kontrol domain'de yapılır.
        if (await products.BarcodeExistsAsync(command.Item.Barcode.Trim(), cancellationToken))
        {
            return Result.Failure<ProductItemDto>(Error.Conflict("Barcode is already in use."));
        }

        if (!string.IsNullOrWhiteSpace(command.Item.Sku)
            && await products.VariantSkuExistsAsync(command.Item.Sku.Trim(), cancellationToken))
        {
            return Result.Failure<ProductItemDto>(Error.Conflict("Variant SKU is already in use."));
        }

        var draftsResult = await ProductCreationSupport.ResolveItemDraftsAsync(
            variantTypes,
            attributes,
            [command.Item],
            cancellationToken);

        if (draftsResult.IsFailure)
        {
            return Result.Failure<ProductItemDto>(draftsResult.Error);
        }

        var addResult = product.AddItem(draftsResult.Value.Single());
        if (addResult.IsFailure)
        {
            return Result.Failure<ProductItemDto>(addResult.Error);
        }

        // Anahtarı domain'de atanan yeni child Modified sanılmasın diye açıkça Added işaretlenir.
        await products.AddItemAsync(addResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(addResult.Value.ToDto(product.Id));
    }
}
