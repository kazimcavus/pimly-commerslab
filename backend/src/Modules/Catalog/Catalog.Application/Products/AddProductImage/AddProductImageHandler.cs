using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.AddProductImage;

/// <summary>Ürün galerisine görsel ekleme işlemini yürüten handler.</summary>
public sealed class AddProductImageHandler(
    IValidator<AddProductImageCommand> validator,
    IProductRepository products,
    IUnitOfWork unitOfWork) : IAddProductImageHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductImageDto>> ExecuteAsync(
        AddProductImageCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductImageDto>(validationResult.Error);
        }

        var product = await products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductImageDto>(Error.NotFound("Product not found."));
        }

        var addResult = product.AddImage(
            command.Url,
            command.SortOrder,
            command.AltText,
            command.IsPrimary,
            command.VariantValueId);

        if (addResult.IsFailure)
        {
            return Result.Failure<ProductImageDto>(addResult.Error);
        }

        products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(addResult.Value.ToDto());
    }
}
