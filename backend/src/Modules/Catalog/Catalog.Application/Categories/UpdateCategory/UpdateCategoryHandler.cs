using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Categories.UpdateCategory;

/// <summary>Kategori güncelleme işlemini yürüten handler.</summary>
public sealed class UpdateCategoryHandler(
    IValidator<UpdateCategoryCommand> validator,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : IUpdateCategoryHandler
{
    /// <inheritdoc/>
    public async Task<Result<CategoryDto>> ExecuteAsync(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<CategoryDto>(validationResult.Error);
        }

        var category = await categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure<CategoryDto>(Error.NotFound("Category not found."));
        }

        if (command.ParentId.HasValue &&
            command.ParentId.Value != category.ParentId &&
            await categories.GetByIdAsync(command.ParentId.Value, cancellationToken) is null)
        {
            return Result.Failure<CategoryDto>(Error.NotFound("Parent category not found."));
        }

        var renameResult = category.Rename(command.Name, command.Code);
        if (renameResult.IsFailure)
        {
            return Result.Failure<CategoryDto>(renameResult.Error);
        }

        var descendants = await categories.GetDescendantIdsAsync(category.Id, cancellationToken);
        var moveResult = category.MoveToParent(command.ParentId, descendants);
        if (moveResult.IsFailure)
        {
            return Result.Failure<CategoryDto>(moveResult.Error);
        }

        categories.Update(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(category.ToDto());
    }
}
