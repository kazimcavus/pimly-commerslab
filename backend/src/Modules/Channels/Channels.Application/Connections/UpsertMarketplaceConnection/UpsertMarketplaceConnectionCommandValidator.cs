using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Connections.UpsertMarketplaceConnection;

/// <summary>UpsertMarketplaceConnectionCommand için doğrulama kuralları.</summary>
public sealed class UpsertMarketplaceConnectionCommandValidator
    : AbstractValidator<UpsertMarketplaceConnectionCommand>
{
    public UpsertMarketplaceConnectionCommandValidator()
    {
        RuleFor(x => x.MarketplaceKey).MarketplaceKey();
        RuleFor(x => x.ApiKey).ApiKey();
        RuleFor(x => x.SellerId).OptionalSellerId();
        RuleFor(x => x.ApiSecret).OptionalApiSecret();
    }
}
