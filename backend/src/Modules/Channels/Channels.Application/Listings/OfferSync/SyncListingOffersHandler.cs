using Channels.Application.Connections;
using Channels.Application.Publications;
using Channels.Domain;
using Channels.Domain.Connections;
using Channels.Domain.Listings;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Channels.Application.Listings.OfferSync;

/// <summary>
/// Bir pazaryerindeki "teklif kirli" listelemelerin fiyat/stok bilgisini toplu olarak pazaryerine gönderir.
/// Olay başına push yerine bu debounce edilmiş toplu tur kullanılır.
/// </summary>
/// <remarks>
/// <para><b>Ön koşullar:</b> Ambient tenant bağlamı kurulmuş olmalı; pazaryerinin etkin bağlantısı olmalı.</para>
/// <para><b>Delta:</b> Her listeleme için güncel fiyat/stoktan hash hesaplanır; saklanan hash ile aynıysa
/// pazaryerine çağrı yapılmaz. Böylece kirlilik bayrağı yanlış pozitif olsa bile trafik üretilmez.</para>
/// <para><b>Hata ayrımı:</b> Taşıma hatası listelemenin durumunu değiştirmez, yalnız backoff kurar;
/// kirlilik korunduğu için sonraki tur doğal olarak yeniden dener.</para>
/// </remarks>
public sealed class SyncListingOffersHandler(
    IProductListingRepository listings,
    IMarketplaceConnectionRepository connections,
    IPricingChannelPriceGateway channelPrices,
    IInventoryStockGateway stocks,
    IMarketplaceOfferClientResolver clientResolver,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<SyncListingOffersHandler> logger) : ISyncListingOffersHandler
{
    /// <summary>Tek turda incelenecek azami kirli listeleme sayısı.</summary>
    private const int PageSize = 1000;

    private static readonly TimeSpan BaseBackoff = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(1);

    /// <inheritdoc/>
    public async Task<Result<OfferSyncSummary>> ExecuteAsync(
        string marketplaceCode,
        CancellationToken cancellationToken = default)
    {
        var marketplaceResult = Marketplace.FromCode(marketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<OfferSyncSummary>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;
        var now = timeProvider.GetUtcNow();

        var dirty = await listings.ListDirtyAsync(marketplace, now, PageSize, cancellationToken);
        var candidates = dirty.Where(listing => listing.OfferDirtyAt is not null).ToList();
        if (candidates.Count == 0)
        {
            return Result.Success(new OfferSyncSummary(0, 0, 0, 0));
        }

        var connection = await connections.GetByMarketplaceAsync(marketplace, cancellationToken);
        if (connection is null || !connection.IsEnabled)
        {
            return Result.Failure<OfferSyncSummary>(
                Error.Validation("Marketplace connection is missing or disabled."));
        }

        var clientResult = clientResolver.Resolve(marketplace);
        if (clientResult.IsFailure)
        {
            return Result.Failure<OfferSyncSummary>(clientResult.Error);
        }

        var client = clientResult.Value;
        var credentials = new MarketplaceCredentials(connection.SellerId, connection.ApiKey, connection.ApiSecret);

        var priceByItem = (await channelPrices.ListForMarketplaceAsync(marketplace, cancellationToken))
            .ToDictionary(price => price.ProductItemId);
        var quantityByItem = await stocks.GetQuantitiesAsync(
            [.. candidates.Select(listing => listing.ProductItemId)],
            cancellationToken);

        var pending = new List<(ProductListing Listing, MarketplaceOfferUpdate Offer, string Hash)>();
        var skipped = 0;

        foreach (var listing in candidates)
        {
            var offer = BuildOffer(listing, priceByItem, quantityByItem);
            if (offer is null)
            {
                // Fiyatı henüz kararlaştırılmamış kalem gönderilemez; kirlilik korunur ki fiyat
                // girildiğinde sonraki tur yakalasın.
                skipped++;
                continue;
            }

            var hash = OfferHasher.Compute(offer);
            if (!listing.NeedsOfferSync(hash))
            {
                // Değer aslında değişmemiş: bayrağı temizle, pazaryerini hiç rahatsız etme.
                listing.MarkOfferSynced(hash, now);
                listings.Update(listing);
                skipped++;
                continue;
            }

            pending.Add((listing, offer, hash));
        }

        var pushed = 0;
        var failed = 0;

        foreach (var batch in Chunk(pending, client.MaxBatchSize))
        {
            var result = await client.UpdateOffersAsync(
                marketplace,
                credentials,
                [.. batch.Select(entry => entry.Offer)],
                cancellationToken);

            foreach (var entry in batch)
            {
                if (result.IsSuccess)
                {
                    entry.Listing.MarkOfferSynced(entry.Hash, now);
                    pushed++;
                }
                else
                {
                    entry.Listing.RegisterSyncFailure(NextAttemptAt(entry.Listing.SyncAttempts, now));
                    failed++;
                }

                listings.Update(entry.Listing);
            }

            if (result.IsFailure)
            {
                logger.LogWarning(
                    "Teklif gönderimi başarısız ({Marketplace}, {Count} kalem): {Error}",
                    marketplace.Code,
                    batch.Count,
                    result.Error.Message);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new OfferSyncSummary(candidates.Count, skipped, pushed, failed));
    }

    private static MarketplaceOfferUpdate? BuildOffer(
        ProductListing listing,
        IReadOnlyDictionary<Guid, DecidedChannelPrice> priceByItem,
        IReadOnlyDictionary<Guid, int> quantityByItem)
    {
        if (listing.ExternalListingId is null)
        {
            return null;
        }

        if (!priceByItem.TryGetValue(listing.ProductItemId, out var price))
        {
            return null;
        }

        // Stok kaydı yoksa kalem tükenmiş sayılır: pazaryerinde de sıfıra çekilmelidir.
        var quantity = quantityByItem.GetValueOrDefault(listing.ProductItemId, 0);

        return new MarketplaceOfferUpdate(
            listing.ExternalListingId,
            quantity,
            price.Amount,
            price.CompareAtAmount,
            price.Currency);
    }

    private static DateTimeOffset NextAttemptAt(int attempts, DateTimeOffset now)
    {
        var factor = Math.Min(attempts, 10);
        var delay = TimeSpan.FromTicks(Math.Min(BaseBackoff.Ticks * (1L << factor), MaxBackoff.Ticks));
        return now + delay;
    }

    private static IEnumerable<List<T>> Chunk<T>(List<T> source, int size)
    {
        var batchSize = Math.Max(1, size);
        for (var index = 0; index < source.Count; index += batchSize)
        {
            yield return source.GetRange(index, Math.Min(batchSize, source.Count - index));
        }
    }
}
