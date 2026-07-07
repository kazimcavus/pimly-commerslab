using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Products.UpsertItemPrice;

/// <summary>UpsertItemPriceCommand için doğrulama kuralları.</summary>
public sealed class UpsertItemPriceCommandValidator : AbstractValidator<UpsertItemPriceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertItemPriceCommandValidator"/> class.
    /// </summary>
    public UpsertItemPriceCommandValidator()
    {
        RuleFor(x => x.ProductItemId).RequiredId();
        RuleFor(x => x.PriceDefinitionId).RequiredId("PriceDefinitionId");
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0)
            .WithMessage("Amount cannot be negative.");
    }
}
