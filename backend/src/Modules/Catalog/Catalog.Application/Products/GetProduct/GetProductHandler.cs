using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Products.GetProduct;

/// <summary>Ürün getirme işlemini yürüten handler.</summary>
public sealed class GetProductHandler(IProductRepository products, IBrandRepository brands) : IGetProductHandler
{
    /// <inheritdoc/>
    public async Task<Result<ProductDto>> ExecuteAsync(
        GetProductQuery query,
        CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(query.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("Product not found."));
        }

        string? brandName = null;
        if (product.BrandId.HasValue)
        {
            var brand = await brands.GetByIdAsync(product.BrandId.Value, cancellationToken);
            brandName = brand?.Name;
        }

        return Result.Success(product.ToDto(brandName));
    }
}
