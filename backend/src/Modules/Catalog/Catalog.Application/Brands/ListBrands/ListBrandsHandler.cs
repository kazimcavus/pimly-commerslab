using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Brands.ListBrands;

/// <summary>Marka listeleme işlemini yürüten handler.</summary>
public sealed class ListBrandsHandler(IBrandRepository brands) : IListBrandsHandler
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<BrandDto>>> ExecuteAsync(
        ListBrandsQuery query,
        CancellationToken cancellationToken = default)
    {
        var paginationResult = PaginationSupport.Resolve(query.Page, query.PageSize);
        if (paginationResult.IsFailure)
        {
            return Result.Failure<PagedResult<BrandDto>>(paginationResult.Error);
        }

        var page = await brands.ListAsync(paginationResult.Value, cancellationToken);
        return Result.Success(page.Map(brand => brand.ToDto()));
    }
}
