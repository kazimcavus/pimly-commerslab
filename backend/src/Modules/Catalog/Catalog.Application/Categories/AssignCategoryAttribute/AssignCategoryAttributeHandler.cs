using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Categories.AssignCategoryAttribute;

/// <summary>Kategoriye özellik atama işlemini yürüten handler.</summary>
public sealed class AssignCategoryAttributeHandler(
    ICategoryRepository categories,
    IAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IAssignCategoryAttributeHandler
{
    /// <inheritdoc/>
    public async Task<Result<CategoryAttributeDto>> ExecuteAsync(
        AssignCategoryAttributeCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure<CategoryAttributeDto>(Error.NotFound("Category not found."));
        }

        var attribute = await attributes.GetByIdAsync(command.AttributeId, cancellationToken);
        if (attribute is null)
        {
            return Result.Failure<CategoryAttributeDto>(Error.NotFound("Attribute not found."));
        }

        var assignResult = category.AssignAttribute(
            command.AttributeId,
            command.Required,
            command.SortOrder);

        if (assignResult.IsFailure)
        {
            return Result.Failure<CategoryAttributeDto>(assignResult.Error);
        }

        categories.Update(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CategoryAttributeMapping.ToDto(assignResult.Value, attribute));
    }
}
