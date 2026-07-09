using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Products.AddProductItem;

/// <summary>AddProductItemCommand için doğrulama kuralları.</summary>
public sealed class AddProductItemCommandValidator : AbstractValidator<AddProductItemCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddProductItemCommandValidator"/> class.
    /// </summary>
    public AddProductItemCommandValidator()
    {
        RuleFor(x => x.ProductId).RequiredId();
        RuleFor(x => x.Item).NotNull()
            .WithMessage("Item payload is required.");
        RuleFor(x => x.Item.Barcode).NotEmpty()
            .WithMessage("Barcode is required.");
        RuleFor(x => x.Item.Stock).GreaterThanOrEqualTo(0)
            .WithMessage("Stock cannot be negative.");
    }
}
