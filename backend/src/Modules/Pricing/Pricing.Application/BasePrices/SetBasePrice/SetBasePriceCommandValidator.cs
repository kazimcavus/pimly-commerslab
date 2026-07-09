using FluentValidation;
using Pricing.Application.Validation;

namespace Pricing.Application.BasePrices.SetBasePrice;

/// <summary>SetBasePriceCommand için doğrulama kuralları.</summary>
public sealed class SetBasePriceCommandValidator : AbstractValidator<SetBasePriceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetBasePriceCommandValidator"/> class.
    /// </summary>
    public SetBasePriceCommandValidator()
    {
        RuleFor(x => x.ProductItemId).RequiredId();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0)
            .WithMessage("Amount cannot be negative.");
        RuleFor(x => x.CompareAtAmount).GreaterThanOrEqualTo(0)
            .When(x => x.CompareAtAmount.HasValue)
            .WithMessage("Compare-at amount cannot be negative.");
    }
}
