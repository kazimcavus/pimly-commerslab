using Channels.Application.Connections;
using Channels.Application.Listings.OfferSync;
using Channels.Application.Publications;
using Channels.Domain;
using Channels.Domain.Connections;
using Channels.Domain.Listings;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Channels.Application.Listings.ContentSync;

/// <summary>
/// Bir pazaryerindeki "içerik kirli" listelemeleri toplu olarak pazaryerine gönderir. Hiç
/// gönderilmemiş (Pending) listelemeler yeni kart olarak, canlı olanlar güncelleme olarak gider.
/// </summary>
/// <remarks>
/// <para><b>Ön koşullar:</b> Ambient tenant bağlamı kurulmuş olmalı; pazaryerinin etkin bağlantısı olmalı.</para>
/// <para><b>Delta:</b> Payload hash'i saklananla aynıysa çağrı yapılmaz — içerik gönderimi ürünü
/// yeniden onaya soktuğu için gereksiz gönderim gerçek zarar verir.</para>
/// <para><b>Hata ayrımı:</b> Ön koşul eksikliği (kategori eşlemesi/fiyat yok) atlanır ve kirlilik
/// korunur; taşıma hatası backoff kurar; içerik reddi ayrı bir akıştır (pazaryeri bildirimi).</para>
/// </remarks>
public sealed class SyncListingContentHandler(
    IProductListingRepository listings,
    IMarketplaceConnectionRepository connections,
    ICatalogListingSourceGateway catalogSources,
    IPricingChannelPriceGateway channelPrices,
    IInventoryStockGateway stocks,
    ListingAssembler assembler,
    IMarketplaceListingClientResolver clientResolver,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<SyncListingContentHandler> logger) : ISyncListingContentHandler
{
    /// <summary>Tek turda incelenecek azami kirli listeleme sayısı.</summary>
    private const int PageSize = 200;

    private static readonly TimeSpan BaseBackoff = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(1);

    /// <inheritdoc/>
    public async Task<Result<ContentSyncSummary>> ExecuteAsync(
        string marketplaceCode,
        CancellationToken cancellationToken = default)
    {
        var marketplaceResult = Marketplace.FromCode(marketplaceCode);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<ContentSyncSummary>(marketplaceResult.Error);
        }

        var marketplace = marketplaceResult.Value;
        var now = timeProvider.GetUtcNow();

        var dirty = await listings.ListDirtyAsync(marketplace, now, PageSize, cancellationToken);
        var candidates = dirty.Where(listing => listing.ContentDirtyAt is not null).ToList();
        if (candidates.Count == 0)
        {
            return Result.Success(new ContentSyncSummary(0, 0, 0, 0, 0));
        }

        var connection = await connections.GetByMarketplaceAsync(marketplace, cancellationToken);
        if (connection is null || !connection.IsEnabled)
        {
            return Result.Failure<ContentSyncSummary>(
                Error.Validation("Marketplace connection is missing or disabled."));
        }

        var clientResult = clientResolver.Resolve(marketplace);
        if (clientResult.IsFailure)
        {
            return Result.Failure<ContentSyncSummary>(clientResult.Error);
        }

        var client = clientResult.Value;
        var credentials = new MarketplaceCredentials(connection.SellerId, connection.ApiKey, connection.ApiSecret);

        var itemIds = candidates.Select(listing => listing.ProductItemId).ToList();
        var sourceByItem = (await catalogSources.GetAsync(itemIds, cancellationToken))
            .ToDictionary(source => source.ProductItemId);
        var priceByItem = (await channelPrices.ListForMarketplaceAsync(marketplace, cancellationToken))
            .ToDictionary(price => price.ProductItemId);
        var quantityByItem = await stocks.GetQuantitiesAsync(itemIds, cancellationToken);

        var pending = new List<PreparedListing>();
        var skipped = 0;

        foreach (var listing in candidates)
        {
            var prepared = await PrepareAsync(
                marketplace,
                listing,
                sourceByItem,
                priceByItem,
                quantityByItem,
                now,
                cancellationToken);

            if (prepared is null)
            {
                skipped++;
                continue;
            }

            pending.Add(prepared);
        }

        var created = 0;
        var updated = 0;
        var failed = 0;

        // Yeni kart ile güncelleme farklı uçlara gider; bu yüzden ayrı gruplanır.
        foreach (var group in pending.GroupBy(entry => entry.IsUpdate))
        {
            foreach (var batch in Chunk([.. group], client.MaxBatchSize))
            {
                var result = await client.SubmitAsync(
                    marketplace,
                    credentials,
                    [.. batch.Select(entry => entry.Request)],
                    group.Key,
                    cancellationToken);

                foreach (var entry in batch)
                {
                    if (result.IsSuccess)
                    {
                        entry.Listing.MarkContentSubmitted(
                            entry.Hash,
                            result.Value.SubmissionReference,
                            now);

                        if (group.Key)
                        {
                            updated++;
                        }
                        else
                        {
                            created++;
                        }
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
                        "İçerik gönderimi başarısız ({Marketplace}, {Count} kalem, güncelleme={IsUpdate}): {Error}",
                        marketplace.Code,
                        batch.Count,
                        group.Key,
                        result.Error.Message);
                }
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ContentSyncSummary(candidates.Count, skipped, created, updated, failed));
    }

    private async Task<PreparedListing?> PrepareAsync(
        Marketplace marketplace,
        ProductListing listing,
        IReadOnlyDictionary<Guid, CatalogListingSource> sourceByItem,
        IReadOnlyDictionary<Guid, DecidedChannelPrice> priceByItem,
        IReadOnlyDictionary<Guid, int> quantityByItem,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!sourceByItem.TryGetValue(listing.ProductItemId, out var source))
        {
            return null;
        }

        if (!priceByItem.TryGetValue(listing.ProductItemId, out var price))
        {
            // Fiyatı kararlaştırılmamış kalem listelenemez; kirlilik korunur ki fiyat girilince
            // sonraki tur yakalasın.
            return null;
        }

        var quantity = quantityByItem.GetValueOrDefault(listing.ProductItemId, 0);

        var assembled = await assembler.AssembleAsync(marketplace, source, price, quantity, cancellationToken);
        if (assembled.IsFailure)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Listeleme atlandı (ItemId={ItemId}): {Reason}",
                    listing.ProductItemId,
                    assembled.Error.Message);
            }

            return null;
        }

        var hash = ContentHasher.Compute(assembled.Value);
        if (!listing.NeedsContentSync(hash))
        {
            // İçerik aslında değişmemiş: bayrağı temizle, pazaryerini yeniden onaya sokma.
            listing.MarkContentSubmitted(hash, listing.SubmissionReference, now);
            listings.Update(listing);
            return null;
        }

        // Dış kimliği olan listeleme pazaryerinde zaten var → güncelleme ucu.
        return new PreparedListing(listing, assembled.Value, hash, listing.ExternalListingId is not null);
    }

    private static DateTimeOffset NextAttemptAt(int attempts, DateTimeOffset now)
    {
        var factor = Math.Min(attempts, 6);
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

    private sealed record PreparedListing(
        ProductListing Listing,
        MarketplaceListingRequest Request,
        string Hash,
        bool IsUpdate);
}
