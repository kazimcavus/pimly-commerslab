using Channels.Application.Connections;
using Channels.Application.Listings.OfferSync;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Channels.Infrastructure.Listings;

/// <summary>
/// Geliştirme/test için teklif gönderimini yalnızca loglayan istemci. Gerçek pazaryerine dokunmadan
/// senkron hattının (kirlilik → delta → gönderim → hash) uçtan uca çalıştığı doğrulanabilir.
/// </summary>
internal sealed class StubMarketplaceOfferClient(ILogger<StubMarketplaceOfferClient> logger)
    : IMarketplaceOfferClient
{
    /// <inheritdoc/>
    public int MaxBatchSize => 100;

    /// <inheritdoc/>
    public Task<Result<OfferUpdateReceipt>> UpdateOffersAsync(
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        IReadOnlyList<MarketplaceOfferUpdate> offers,
        CancellationToken cancellationToken = default)
    {
        _ = credentials;
        cancellationToken.ThrowIfCancellationRequested();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[stub] {Marketplace} teklif güncellemesi: {Count} kalem ({Barcodes}).",
                marketplace.Code,
                offers.Count,
                string.Join(", ", offers.Take(5).Select(offer => offer.ExternalListingId)));
        }

        return Task.FromResult(Result.Success(
            new OfferUpdateReceipt($"stub-batch-{Guid.NewGuid():N}")));
    }
}
