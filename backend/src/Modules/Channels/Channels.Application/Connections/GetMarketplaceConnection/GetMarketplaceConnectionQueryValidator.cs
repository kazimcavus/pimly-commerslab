using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Connections.GetMarketplaceConnection;

/// <summary>GetMarketplaceConnectionQuery için doğrulama kuralları.</summary>
public sealed class GetMarketplaceConnectionQueryValidator : AbstractValidator<GetMarketplaceConnectionQuery>
{
    public GetMarketplaceConnectionQueryValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
    }
}
