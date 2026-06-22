using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Attributes.ListAttributeValues;

/// <summary>Özellik değerlerini listeleme işlemini gerçekleştirir.</summary>
public sealed class ListAttributeValuesHandler(IAttributeRepository attributes) : IListAttributeValuesHandler
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<AttributeDefinitionValueDto>>> ExecuteAsync(
        ListAttributeValuesQuery query,
        CancellationToken cancellationToken = default)
    {
        var paginationResult = PaginationSupport.Resolve(query.Page, query.PageSize);
        if (paginationResult.IsFailure)
        {
            return Result.Failure<PagedResult<AttributeDefinitionValueDto>>(paginationResult.Error);
        }

        var attribute = await attributes.GetByIdAsync(query.AttributeId, cancellationToken);
        if (attribute is null)
        {
            return Result.Failure<PagedResult<AttributeDefinitionValueDto>>(Error.NotFound("Attribute not found."));
        }

        var values = attribute.Values
            .OrderBy(v => v.Name)
            .Select(v => v.ToDto(attribute.Id))
            .ToList();

        return Result.Success(PagedResult<AttributeDefinitionValueDto>.FromAll(values, paginationResult.Value));
    }
}
