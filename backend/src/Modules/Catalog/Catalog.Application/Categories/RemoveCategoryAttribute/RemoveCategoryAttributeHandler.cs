using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Categories.RemoveCategoryAttribute;

/// <summary>Kategori-özellik atamasını kaldırma işlemini yürüten handler.</summary>
public sealed class RemoveCategoryAttributeHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : IRemoveCategoryAttributeHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        RemoveCategoryAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        var owner = await CategoryAssignmentLookup.FindByAssignmentIdAsync(
            categories,
            command.Id,
            cancellationToken);

        if (owner is null)
        {
            return Result.Failure(Error.NotFound("Category attribute assignment not found."));
        }

        var removeResult = owner.RemoveAssignment(command.Id);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        categories.Update(owner);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
