using Channels.Application.Listings.OfferSync;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Channels.Infrastructure.Listings;

/// <summary>Keyed DI ile pazaryeri teklif (fiyat/stok) istemcisi çözümlemesi.</summary>
internal sealed class MarketplaceOfferClientResolver(IServiceProvider serviceProvider)
    : IMarketplaceOfferClientResolver
{
    /// <inheritdoc/>
    public Result<IMarketplaceOfferClient> Resolve(Marketplace marketplace)
    {
        var client = serviceProvider.GetKeyedService<IMarketplaceOfferClient>(marketplace.Code);
        return client is null
            ? Result.Failure<IMarketplaceOfferClient>(
                Error.NotFound($"Offer client is not configured for marketplace '{marketplace.Code}'."))
            : Result.Success(client);
    }
}
