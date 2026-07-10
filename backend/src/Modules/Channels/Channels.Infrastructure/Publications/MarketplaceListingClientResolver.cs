using Channels.Application.Publications;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Channels.Infrastructure.Publications;

/// <summary>Keyed DI ile pazaryeri listeleme (publish) istemcisi çözümlemesi.</summary>
internal sealed class MarketplaceListingClientResolver(IServiceProvider serviceProvider)
    : IMarketplaceListingClientResolver
{
    /// <inheritdoc/>
    public Result<IMarketplaceListingClient> Resolve(Marketplace marketplace)
    {
        var client = serviceProvider.GetKeyedService<IMarketplaceListingClient>(marketplace.Code);
        return client is null
            ? Result.Failure<IMarketplaceListingClient>(
                Error.NotFound($"Listing client is not configured for marketplace '{marketplace.Code}'."))
            : Result.Success(client);
    }
}
