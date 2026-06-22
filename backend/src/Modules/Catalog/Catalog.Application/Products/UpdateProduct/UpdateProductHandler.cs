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
            command.Name,
            status,
            command.AttributeValueInputs is null ? null : attributeValuesResult.Value);

        if (updateResult.IsFailure)
        {
            return Result.Failure<ProductDto>(updateResult.Error);
        }

        products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(product.ToDto());
    }
}
