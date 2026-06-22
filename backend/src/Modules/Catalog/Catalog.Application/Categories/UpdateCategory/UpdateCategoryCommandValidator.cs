using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Categories.UpdateCategory;

/// <summary>UpdateCategoryCommand için doğrulama kuralları.</summary>
public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCategoryCommandValidator"/> class.
    /// </summary>
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).RequiredId();
        RuleFor(x => x.Name).CategoryName();
        RuleFor(x => x.Code).CategoryCode();
    }
}
