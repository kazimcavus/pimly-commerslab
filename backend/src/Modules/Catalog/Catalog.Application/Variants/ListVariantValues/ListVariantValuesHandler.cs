using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Variants.ListVariantValues;

/// <summary>Varyant türü değerlerini listeleme işlemini gerçekleştirir.</summary>
public sealed class ListVariantValuesHandler(IVariantRepository variantTypes) : IListVariantValuesHandler
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<VariantValueDto>>> ExecuteAsync(
        ListVariantValuesQuery query,
        CancellationToken cancellationToken = default)
    {
        var paginationResult = PaginationSupport.Resolve(query.Page, query.PageSize);
        if (paginationResult.IsFailure)
        {
            return Result.Failure<PagedResult<VariantValueDto>>(paginationResult.Error);
        }

        var variantType = await variantTypes.GetByIdAsync(query.VariantTypeId, cancellationToken);
        if (variantType is null)
        {
            return Result.Failure<PagedResult<VariantValueDto>>(Error.NotFound("Variant type not found."));
        }

        var values = variantType.Values
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Label)
            .Select(v => v.ToDto(variantType.Id))
            .ToList();

        return Result.Success(PagedResult<VariantValueDto>.FromAll(values, paginationResult.Value));
    }
}
