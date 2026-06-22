using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.Categories;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.Categories.CreateCategory;

/// <summary>Yeni kategori oluşturma işlemini yürüten handler.</summary>
public sealed class CreateCategoryHandler(
    IValidator<CreateCategoryCommand> validator,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : ICreateCategoryHandler
{
    /// <inheritdoc/>
    public async Task<Result<CategoryDto>> ExecuteAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<CategoryDto>(validationResult.Error);
        }

        if (command.ParentId.HasValue &&
            await categories.GetByIdAsync(command.ParentId.Value, cancellationToken) is null)
        {
            return Result.Failure<CategoryDto>(Error.NotFound("Parent category not found."));
        }

        var createResult = Category.Create(command.Name, command.Code, command.ParentId);
        if (createResult.IsFailure)
        {
            return Result.Failure<CategoryDto>(createResult.Error);
        }

        await categories.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(createResult.Value.ToDto());
    }
}
