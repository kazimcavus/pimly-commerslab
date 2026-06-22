using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Categories.DeleteCategory;

/// <summary>Kategori silme işlemini yürüten handler.</summary>
public sealed class DeleteCategoryHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : IDeleteCategoryHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure(Error.NotFound("Category not found."));
        }

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
