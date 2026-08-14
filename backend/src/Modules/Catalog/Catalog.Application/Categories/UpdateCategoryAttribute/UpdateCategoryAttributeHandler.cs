using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Categories.UpdateCategoryAttribute;

/// <summary>Kategori-özellik atamasını güncelleme işlemini yürüten handler.</summary>
public sealed class UpdateCategoryAttributeHandler(
    ICategoryRepository categories,
    IAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IUpdateCategoryAttributeHandler
{
    /// <inheritdoc/>
    public async Task<Result<CategoryAttributeDto>> ExecuteAsync(
        UpdateCategoryAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        var owner = await CategoryAssignmentLookup.FindByAssignmentIdAsync(
            categories,
            command.Id,
            cancellationToken);

        if (owner is null)
        {
            return Result.Failure<CategoryAttributeDto>(Error.NotFound("Category attribute assignment not found."));
        }

        var assignment = owner.Assignments.First(a => a.Id == command.Id);
        var updateResult = owner.UpdateAssignment(
            command.Id,
            command.Required,
            command.SortOrder,
            command.Scope);

        if (updateResult.IsFailure)
        {
            return Result.Failure<CategoryAttributeDto>(updateResult.Error);
        }

        categories.Update(owner);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var attribute = await attributes.GetByIdAsync(assignment.AttributeId, cancellationToken);
        if (attribute is null)
        {
            return Result.Failure<CategoryAttributeDto>(Error.NotFound("Attribute not found."));
        }

        return Result.Success(CategoryAttributeMapping.ToDto(assignment, attribute));
    }
}
