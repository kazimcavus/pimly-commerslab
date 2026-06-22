using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Categories.CreateCategory;

/// <summary>CreateCategoryCommand için doğrulama kuralları.</summary>
public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCategoryCommandValidator"/> class.
    /// </summary>
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).CategoryName();
        RuleFor(x => x.Code).CategoryCode();
    }
}
