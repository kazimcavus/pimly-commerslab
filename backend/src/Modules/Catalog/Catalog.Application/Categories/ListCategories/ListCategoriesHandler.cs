using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Categories.ListCategories;

/// <summary>Kategori listeleme işlemini yürüten handler.</summary>
public sealed class ListCategoriesHandler(ICategoryRepository categories) : IListCategoriesHandler
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<CategoryDto>>> ExecuteAsync(
        ListCategoriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var paginationResult = PaginationSupport.Resolve(query.Page, query.PageSize);
        if (paginationResult.IsFailure)
        {
            return Result.Failure<PagedResult<CategoryDto>>(paginationResult.Error);
        }

        var page = await categories.ListAsync(paginationResult.Value, cancellationToken);
        return Result.Success(page.Map(category => category.ToDto()));
    }
}
