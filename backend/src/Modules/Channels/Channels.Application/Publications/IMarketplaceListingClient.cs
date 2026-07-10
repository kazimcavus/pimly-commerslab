using Channels.Application.Connections;
using SharedKernel;

namespace Channels.Application.Publications;

/// <summary>Bir satılabilir kalemi pazaryerinde listeleyen (publish) istemci.</summary>
public interface IMarketplaceListingClient
{
    /// <summary>Kalemi pazaryerine gönderir; dış listeleme kimliğini döner.</summary>
    Task<Result<PublishedListing>> PublishAsync(
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        MarketplaceListingRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Pazaryerine gönderilecek listeleme isteği (minimal; fiyat kararı Pricing'den gelir).</summary>
public sealed record MarketplaceListingRequest(
    Guid ProductItemId,
    decimal Amount,
    decimal? CompareAtAmount,
    string Currency);

/// <summary>Pazaryerinde oluşturulan/güncellenen listelemenin sonucu.</summary>
public sealed record PublishedListing(Guid ProductItemId, string ExternalListingId);
