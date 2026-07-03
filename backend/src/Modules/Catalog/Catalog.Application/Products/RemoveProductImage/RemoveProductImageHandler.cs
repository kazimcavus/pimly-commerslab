using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Products.RemoveProductImage;

/// <summary>Ürün galerisi görseli silme işlemini yürüten handler.</summary>
public sealed class RemoveProductImageHandler(
    IValidator<RemoveProductImageCommand> validator,
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRemoveProductImageHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        RemoveProductImageCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        var product = await products.GetByImageIdAsync(command.ImageId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(Error.NotFound("Product image not found."));
        }

        var removeResult = product.RemoveImage(command.ImageId);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
