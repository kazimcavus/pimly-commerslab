using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Products.UpdateProduct;

/// <summary>UpdateProductCommand için doğrulama kuralları.</summary>
public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProductCommandValidator"/> class.
    /// </summary>
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).RequiredId();
        RuleFor(x => x.CategoryId).RequiredCategoryId();
        RuleFor(x => x.Name).ProductName();
        RuleFor(x => x.Status).ProductStatus();
    }
}
