using Catalog.Application.Validation;
using FluentValidation;

namespace Catalog.Application.Products.DeleteItemChannelPrice;

/// <summary>DeleteItemChannelPriceCommand için doğrulama kuralları.</summary>
public sealed class DeleteItemChannelPriceCommandValidator : AbstractValidator<DeleteItemChannelPriceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteItemChannelPriceCommandValidator"/> class.
    /// </summary>
    public DeleteItemChannelPriceCommandValidator()
    {
        RuleFor(x => x.ProductItemId).RequiredId();
        RuleFor(x => x.MarketplaceKey)
            .NotEmpty()
            .WithMessage("Marketplace key is required.");
    }
}
