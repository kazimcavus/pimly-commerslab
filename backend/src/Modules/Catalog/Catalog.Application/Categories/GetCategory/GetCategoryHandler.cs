using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Categories.GetCategory;

/// <summary>Kategori getirme işlemini yürüten handler.</summary>
public sealed class GetCategoryHandler(ICategoryRepository categories) : IGetCategoryHandler
{
    /// <inheritdoc/>
    public async Task<Result<CategoryDto>> ExecuteAsync(
        GetCategoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(query.Id, cancellationToken);
        return category is null
            ? Result.Failure<CategoryDto>(Error.NotFound("Category not found."))
            : Result.Success(category.ToDto());
    }
}
