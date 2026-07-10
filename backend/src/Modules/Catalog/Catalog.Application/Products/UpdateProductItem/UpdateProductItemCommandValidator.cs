using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Products.UpdateProductItem;

/// <summary>UpdateProductItemCommand için doğrulama kuralları.</summary>
public sealed class UpdateProductItemCommandValidator : AbstractValidator<UpdateProductItemCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProductItemCommandValidator"/> class.
    /// </summary>
    public UpdateProductItemCommandValidator()
    {
        RuleFor(x => x.Id).RequiredId();
    }
}
