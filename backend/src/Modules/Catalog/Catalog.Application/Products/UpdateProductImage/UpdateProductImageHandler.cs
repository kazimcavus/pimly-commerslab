using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.UpdateProductImage;

/// <summary>Ürün galerisi görseli güncelleme işlemini yürüten handler.</summary>
public sealed class UpdateProductImageHandler(
    IValidator<UpdateProductImageCommand> validator,
    IProductRepository products,
    IUnitOfWork unitOfWork) : IUpdateProductImageHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductImageDto>> ExecuteAsync(
        UpdateProductImageCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductImageDto>(validationResult.Error);
        }

        var product = await products.GetByImageIdAsync(command.ImageId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductImageDto>(Error.NotFound("Product image not found."));
        }

        var updateResult = product.UpdateImage(
            command.ImageId,
            command.Url,
            command.SortOrder,
            command.AltText,
            command.IsPrimary,
            command.VariantValueId);

        if (updateResult.IsFailure)
        {
            return Result.Failure<ProductImageDto>(updateResult.Error);
        }

        products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var image = product.Images.First(i => i.Id == command.ImageId);
        return Result.Success(image.ToDto());
    }
}
