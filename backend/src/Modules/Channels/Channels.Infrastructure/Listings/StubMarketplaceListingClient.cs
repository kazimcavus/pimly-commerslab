using Channels.Application.Connections;
using Channels.Application.Listings.ContentSync;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Channels.Infrastructure.Listings;

/// <summary>
/// Geliştirme/test için ürün kartı gönderimini yalnızca loglayan istemci. Gerçek pazaryerine
/// dokunmadan içerik hattının (kirlilik → assembler → delta → gönderim) uçtan uca çalıştığı
/// doğrulanabilir.
/// </summary>
internal sealed class StubMarketplaceListingClient(ILogger<StubMarketplaceListingClient> logger)
    : IMarketplaceListingClient
{
    /// <inheritdoc/>
    public int MaxBatchSize => 100;

    /// <inheritdoc/>
    public Task<Result<ListingSubmissionReceipt>> SubmitAsync(
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        IReadOnlyList<MarketplaceListingRequest> listings,
        bool isUpdate,
        CancellationToken cancellationToken = default)
    {
        _ = credentials;
        cancellationToken.ThrowIfCancellationRequested();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[stub] {Marketplace} ürün kartı {Mode}: {Count} kalem ({Barcodes}).",
                marketplace.Code,
                isUpdate ? "güncelleme" : "oluşturma",
                listings.Count,
                string.Join(", ", listings.Take(5).Select(listing => listing.Barcode)));
        }

        return Task.FromResult(Result.Success(
            new ListingSubmissionReceipt($"stub-listing-{Guid.NewGuid():N}")));
    }
}
