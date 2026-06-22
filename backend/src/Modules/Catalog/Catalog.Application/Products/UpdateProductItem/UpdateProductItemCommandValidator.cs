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
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0)
            .WithMessage("Price cannot be negative.");
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0)
            .WithMessage("Stock cannot be negative.");
    }
}
