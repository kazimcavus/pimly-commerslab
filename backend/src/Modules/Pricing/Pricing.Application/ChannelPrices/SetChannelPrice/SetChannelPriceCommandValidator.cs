using FluentValidation;
using Pricing.Application.Validation;

namespace Pricing.Application.ChannelPrices.SetChannelPrice;

/// <summary>SetChannelPriceCommand için doğrulama kuralları.</summary>
public sealed class SetChannelPriceCommandValidator : AbstractValidator<SetChannelPriceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetChannelPriceCommandValidator"/> class.
    /// </summary>
    public SetChannelPriceCommandValidator()
    {
        RuleFor(x => x.ProductItemId).RequiredId();
        RuleFor(x => x.Marketplace).NotEmpty()
            .WithMessage("Marketplace is required.");
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0)
            .WithMessage("Amount cannot be negative.");
        RuleFor(x => x.CompareAtAmount).GreaterThanOrEqualTo(0)
            .When(x => x.CompareAtAmount.HasValue)
            .WithMessage("Compare-at amount cannot be negative.");
    }
}
