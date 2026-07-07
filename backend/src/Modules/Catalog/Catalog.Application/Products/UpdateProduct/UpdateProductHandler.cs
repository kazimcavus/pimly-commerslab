using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.UpdateProduct;

/// <summary>Ürün güncelleme işlemini yürüten handler.</summary>
public sealed class UpdateProductHandler(
    IValidator<UpdateProductCommand> validator,
    IProductRepository products,
    ICategoryRepository categories,
    IBrandRepository brands,
    IAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IUpdateProductHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductDto>> ExecuteAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductDto>(validationResult.Error);
        }

        var product = await products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("Product not found."));
        }

        var category = await categories.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("Category not found."));
        }

        // Null girdi mevcut değerleri koruduğu için zorunluluk, güncelleme sonrası geçerli
        // olacak öznitelik kümesi üzerinden denetlenir.
        var providedAttributeIds = command.AttributeValueInputs is null
            ? product.AttributeValues.Select(value => value.Attribute.Id).ToHashSet()
            : command.AttributeValueInputs.Select(input => input.AttributeId).ToHashSet();

        var requiredAttributesResult = await ProductCreationSupport.EnsureRequiredCategoryAttributesAsync(
            attributes,
            category,
            providedAttributeIds,
            cancellationToken);

        if (requiredAttributesResult.IsFailure)
        {
            return Result.Failure<ProductDto>(requiredAttributesResult.Error);
        }

        string? brandName = null;
        if (command.BrandId.HasValue)
        {
            var brand = await brands.GetByIdAsync(command.BrandId.Value, cancellationToken);
            if (brand is null)
            {
                return Result.Failure<ProductDto>(Error.NotFound("Brand not found."));
            }

            brandName = brand.Name;
        }

        var attributeValuesResult = await ProductCreationSupport.ResolveAttributeValuesAsync(
            attributes,
            command.AttributeValueInputs,
            cancellationToken);

        if (attributeValuesResult.IsFailure)
        {
            return Result.Failure<ProductDto>(attributeValuesResult.Error);
        }

        var status = ProductMappings.ParseStatus(command.Status);
        var updateResult = product.UpdateDetails(
            command.CategoryId,
            command.Name,
            status,
            command.AttributeValueInputs is null ? null : attributeValuesResult.Value,
            command.BrandId,
            command.Description);

        if (updateResult.IsFailure)
        {
            return Result.Failure<ProductDto>(updateResult.Error);
        }

        products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(product.ToDto(brandName));
    }
}
