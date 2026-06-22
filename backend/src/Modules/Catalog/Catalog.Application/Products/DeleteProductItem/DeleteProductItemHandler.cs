using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Products.DeleteProductItem;

/// <summary>Ürün varyantı silme işlemini yürüten handler.</summary>
public sealed class DeleteProductItemHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IDeleteProductItemHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteProductItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await products.GetByItemIdAsync(command.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure(Error.NotFound("Product variant not found."));
        }

        var removeResult = product.RemoveItem(command.Id);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        products.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
