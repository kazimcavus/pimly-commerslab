using Catalog.Application.Validation;
using Catalog.Domain.Products;
using FluentValidation;

namespace Catalog.Application.Products.UpsertItemChannelPrice;

/// <summary>UpsertItemChannelPriceCommand için doğrulama kuralları.</summary>
public sealed class UpsertItemChannelPriceCommandValidator : AbstractValidator<UpsertItemChannelPriceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertItemChannelPriceCommandValidator"/> class.
    /// </summary>
    public UpsertItemChannelPriceCommandValidator()
    {
        RuleFor(x => x.ProductItemId).RequiredId();
        RuleFor(x => x.MarketplaceKey)
            .NotEmpty()
            .WithMessage("Marketplace key is required.")
            .MaximumLength(ProductItemChannelPrice.MarketplaceKeyMaxLength)
            .WithMessage($"Marketplace key cannot exceed {ProductItemChannelPrice.MarketplaceKeyMaxLength} characters.");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0)
            .WithMessage("Price cannot be negative.");
        RuleFor(x => x.CompareAtPrice).GreaterThanOrEqualTo(0)
            .When(x => x.CompareAtPrice.HasValue)
            .WithMessage("Compare at price cannot be negative.");
    }
}
