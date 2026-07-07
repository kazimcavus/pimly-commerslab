using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Products.ListProducts;

/// <summary>Ürünleri sayfalı biçimde listeleme işlemini gerçekleştirir.</summary>
public sealed class ListProductsHandler(IProductRepository products, IBrandRepository brands) : IListProductsHandler
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<ProductDto>>> ExecuteAsync(
        ListProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        var paginationResult = PaginationSupport.Resolve(query.Page, query.PageSize);
        if (paginationResult.IsFailure)
        {
            return Result.Failure<PagedResult<ProductDto>>(paginationResult.Error);
        }

        var page = await products.ListAsync(paginationResult.Value, cancellationToken);

        var brandNamesById = (await brands.ListAsync(cancellationToken))
            .ToDictionary(brand => brand.Id, brand => brand.Name);

        return Result.Success(page.Map(product => product.ToDto(
            product.BrandId.HasValue && brandNamesById.TryGetValue(product.BrandId.Value, out var brandName)
                ? brandName
                : null)));
    }
}
