using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Products;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.CreateProduct;

/// <summary>Tek ürün oluşturma işlemini yürüten handler.</summary>
public sealed class CreateProductHandler(
    IValidator<CreateProductCommand> validator,
    IProductRepository products,
    IVariantRepository variantTypes,
    IAttributeRepository attributes,
    IUnitOfWork unitOfWork) : ICreateProductHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductDto>> ExecuteAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductDto>(validationResult.Error);
        }

        var resolvedTypesResult = await ProductCreationSupport.ResolveVariantsAsync(
            variantTypes,
            command.Variants,
            cancellationToken);

        if (resolvedTypesResult.IsFailure)
        {
            return Result.Failure<ProductDto>(resolvedTypesResult.Error);
        }

        if (resolvedTypesResult.Value.Any(type => type.Slicer))
        {
            return Result.Failure<ProductDto>(
                Error.Validation("Products with a slicer variant type must be created using POST /products:batch."));
        }

        var attributeValuesResult = await ProductCreationSupport.ResolveAttributeValuesAsync(
            attributes,
            command.AttributeValueInputs,
            cancellationToken);

        if (attributeValuesResult.IsFailure)
        {
            return Result.Failure<ProductDto>(attributeValuesResult.Error);
        }

        var itemDraftsResult = await ProductCreationSupport.ResolveItemDraftsAsync(
            variantTypes,
            attributes,
            command.Items,
            cancellationToken);

        if (itemDraftsResult.IsFailure)
        {
            return Result.Failure<ProductDto>(itemDraftsResult.Error);
        }

        var itemDrafts = itemDraftsResult.Value;

        if (await products.ModelCodeExistsAsync(command.ModelCode, cancellationToken))
        {
            return Result.Failure<ProductDto>(Error.Conflict("Model code already exists."));
        }

        foreach (var item in itemDrafts)
        {
            if (await products.BarcodeExistsAsync(item.Barcode, cancellationToken))
            {
                return Result.Failure<ProductDto>(Error.Conflict($"Barcode '{item.Barcode}' already exists."));
            }

            if (!string.IsNullOrWhiteSpace(item.Sku) &&
                await products.VariantSkuExistsAsync(item.Sku, cancellationToken))
            {
                return Result.Failure<ProductDto>(Error.Conflict($"Variant SKU '{item.Sku}' already exists."));
            }
        }

        var status = ProductMappings.ParseStatus(command.Status);
        var createResult = Product.Create(
            command.GroupId,
            command.ModelCode,
            command.Name,
            status,
            attributeValuesResult.Value,
            resolvedTypesResult.Value,
            itemDrafts.ToList());

        if (createResult.IsFailure)
        {
            return Result.Failure<ProductDto>(createResult.Error);
        }

        await products.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(createResult.Value.ToDto());
    }
}
