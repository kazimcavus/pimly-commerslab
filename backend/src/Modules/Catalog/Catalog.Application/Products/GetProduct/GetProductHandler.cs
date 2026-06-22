using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Products.GetProduct;

/// <summary>Ürün getirme işlemini yürüten handler.</summary>
public sealed class GetProductHandler(IProductRepository products) : IGetProductHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductDto>> ExecuteAsync(
        GetProductQuery query,
        CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(query.Id, cancellationToken);
        return product is null
            ? Result.Failure<ProductDto>(Error.NotFound("Product not found."))
            : Result.Success(product.ToDto());
    }
}
