using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Variants.ListVariantTypes;

/// <summary>Varyant türlerini listeleme işlemini gerçekleştirir.</summary>
public sealed class ListVariantTypesHandler(IVariantRepository variantTypes) : IListVariantTypesHandler
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<VariantTypeDto>>> ExecuteAsync(
        ListVariantTypesQuery query,
        CancellationToken cancellationToken = default)
    {
        var paginationResult = PaginationSupport.Resolve(query.Page, query.PageSize);
        if (paginationResult.IsFailure)
        {
            return Result.Failure<PagedResult<VariantTypeDto>>(paginationResult.Error);
        }

        var page = await variantTypes.ListAsync(paginationResult.Value, cancellationToken);
        return Result.Success(page.Map(variantType => variantType.ToDto()));
    }
}
