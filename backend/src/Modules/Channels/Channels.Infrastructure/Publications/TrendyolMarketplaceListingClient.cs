using Channels.Application.Connections;
using Channels.Application.Publications;
using SharedKernel;

namespace Channels.Infrastructure.Publications;

/// <summary>
/// Trendyol listeleme (publish) istemcisi iskeleti. Gerçek Trendyol listeleme API entegrasyonu
/// sonraki bir dilimde eklenecektir; şimdilik açık bir hata döner (stub kompozisyonu kullanılır).
/// </summary>
internal sealed class TrendyolMarketplaceListingClient : IMarketplaceListingClient
{
    /// <inheritdoc/>
    public Task<Result<PublishedListing>> PublishAsync(
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        MarketplaceListingRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = marketplace;
        _ = credentials;
        _ = request;
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Result.Failure<PublishedListing>(
            Error.Failure("Trendyol listing publish is not implemented yet.")));
    }
}
