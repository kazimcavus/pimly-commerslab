using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Attributes.ListAttributes;

/// <summary>Öznitelikleri listeleme işlemini gerçekleştirir.</summary>
public sealed class ListAttributesHandler(IAttributeRepository attributes) : IListAttributesHandler
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<AttributeDto>>> ExecuteAsync(
        ListAttributesQuery query,
        CancellationToken cancellationToken = default)
    {
        var paginationResult = PaginationSupport.Resolve(query.Page, query.PageSize);
        if (paginationResult.IsFailure)
        {
            return Result.Failure<PagedResult<AttributeDto>>(paginationResult.Error);
        }

        var page = await attributes.ListAsync(paginationResult.Value, cancellationToken);
        return Result.Success(page.Map(attribute => attribute.ToDto()));
    }
}
