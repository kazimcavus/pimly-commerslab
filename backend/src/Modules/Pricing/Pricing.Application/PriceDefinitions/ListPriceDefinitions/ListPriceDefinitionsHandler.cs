using Pricing.Application.Contracts;
using Pricing.Domain.PriceDefinitions;
using SharedKernel;

namespace Pricing.Application.PriceDefinitions.ListPriceDefinitions;

/// <summary>Fiyat tanımı listeleme işlemini yürüten handler.</summary>
public sealed class ListPriceDefinitionsHandler(IPriceDefinitionRepository priceDefinitions) : IListPriceDefinitionsHandler
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<PriceDefinitionDto>>> ExecuteAsync(
        ListPriceDefinitionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var paginationResult = PaginationSupport.Resolve(query.Page, query.PageSize);
        if (paginationResult.IsFailure)
        {
            return Result.Failure<PagedResult<PriceDefinitionDto>>(paginationResult.Error);
        }

        var page = await priceDefinitions.ListAsync(paginationResult.Value, cancellationToken);
        return Result.Success(page.Map(definition => definition.ToDto()));
    }
}
