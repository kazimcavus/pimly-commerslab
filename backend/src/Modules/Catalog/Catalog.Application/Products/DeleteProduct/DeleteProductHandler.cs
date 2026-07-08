using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Products.DeleteProduct;

/// <summary>Ürün silme işlemini yürüten handler.</summary>
public sealed class DeleteProductHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IDeleteProductHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure(Error.NotFound("Product not found."));
        }

        product.PrepareForRemoval();
        products.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
