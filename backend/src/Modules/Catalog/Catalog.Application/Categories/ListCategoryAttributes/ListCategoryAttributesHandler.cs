using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Categories.ListCategoryAttributes;

/// <summary>Kategori özelliklerini listeleme işlemini yürüten handler.</summary>
public sealed class ListCategoryAttributesHandler(
    ICategoryRepository categories,
    IAttributeRepository attributes) : IListCategoryAttributesHandler
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<CategoryAttributeDto>>> ExecuteAsync(
        ListCategoryAttributesQuery query,
        CancellationToken cancellationToken = default)
    {
        var paginationResult = PaginationSupport.Resolve(query.Page, query.PageSize);
        if (paginationResult.IsFailure)
        {
            return Result.Failure<PagedResult<CategoryAttributeDto>>(paginationResult.Error);
        }

        var category = await categories.GetByIdAsync(query.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure<PagedResult<CategoryAttributeDto>>(Error.NotFound("Category not found."));
        }

        var attributeIds = category.Assignments.Select(a => a.AttributeId).ToHashSet();
        var attributeMap = (await attributes.ListAsync(cancellationToken))
            .Where(a => attributeIds.Contains(a.Id))
            .ToDictionary(a => a.Id);

        var rows = category.Assignments
            .OrderBy(a => a.SortOrder)
            .Where(assignment => attributeMap.ContainsKey(assignment.AttributeId))
            .Select(assignment => CategoryAttributeMapping.ToDto(assignment, attributeMap[assignment.AttributeId]))
            .ToList();

        return Result.Success(PagedResult<CategoryAttributeDto>.FromAll(rows, paginationResult.Value));
    }
}
