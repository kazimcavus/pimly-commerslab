using Channels.Application.Connections;
using Channels.Application.Publications;
using SharedKernel;

namespace Channels.Infrastructure.Publications;

/// <summary>
/// Geliştirme/test için deterministik sahte listeleme istemcisi. Ağ erişimi yapmaz; her kalemi
/// başarıyla "yayımlar" ve dış listeleme kimliği olarak "{MP}-{itemId}" döner.
/// </summary>
internal sealed class StubMarketplaceListingClient : IMarketplaceListingClient
{
    /// <inheritdoc/>
    public Task<Result<PublishedListing>> PublishAsync(
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        MarketplaceListingRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = credentials;
        cancellationToken.ThrowIfCancellationRequested();

        var externalId = $"{marketplace.Code}-{request.ProductItemId:N}";
        return Task.FromResult(Result.Success(new PublishedListing(request.ProductItemId, externalId)));
    }
}
