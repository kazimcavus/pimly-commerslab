using Channels.Domain.Listings;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Channels.Infrastructure.Repositories;

/// <summary>ProductListing aggregate'leri için EF Core tabanlı depo.</summary>
/// <remarks>
/// Listeleme kayıtları tenant query filter'ına dahildir; yalnızca <see cref="ListDirtyScopesAsync"/>
/// filtreyi bilinçli olarak devre dışı bırakır (worker tenant bağlamı olmadan keşif yapar).
/// </remarks>
internal sealed class ProductListingRepository(ChannelsDbContext db) : IProductListingRepository
{
    public Task<ProductListing?> GetAsync(
        Marketplace marketplace,
        Guid productItemId,
        CancellationToken cancellationToken = default) =>
        db.ProductListings.FirstOrDefaultAsync(
            listing => listing.Marketplace == marketplace && listing.ProductItemId == productItemId,
            cancellationToken);

    public async Task<IReadOnlyList<ProductListing>> ListByProductItemAsync(
        Guid productItemId,
        CancellationToken cancellationToken = default) =>
        await db.ProductListings
            .Where(listing => listing.ProductItemId == productItemId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductListing>> ListByProductItemsAsync(
        Marketplace marketplace,
        IReadOnlyCollection<Guid> productItemIds,
        CancellationToken cancellationToken = default)
    {
        if (productItemIds.Count == 0)
        {
            return [];
        }

        var ids = productItemIds as Guid[] ?? [.. productItemIds];

        return await db.ProductListings
            .Where(listing => listing.Marketplace == marketplace && ids.Contains(listing.ProductItemId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductListing>> ListDirtyAsync(
        Marketplace marketplace,
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken = default) =>
        await db.ProductListings
            .Where(listing =>
                listing.Marketplace == marketplace
                && (listing.ContentDirtyAt != null || listing.OfferDirtyAt != null)
                && (listing.NextAttemptAt == null || listing.NextAttemptAt <= now))
            .OrderBy(listing => listing.OfferDirtyAt ?? listing.ContentDirtyAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ListingSyncScope>> ListDirtyScopesAsync(
        IReadOnlyCollection<Guid>? tenantIds,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var query = db.ProductListings
            .IgnoreQueryFilters()
            .Where(listing =>
                (listing.ContentDirtyAt != null || listing.OfferDirtyAt != null)
                && (listing.NextAttemptAt == null || listing.NextAttemptAt <= now));

        if (tenantIds is { Count: > 0 })
        {
            var filter = tenantIds as Guid[] ?? [.. tenantIds];
            query = query.Where(listing => filter.Contains(listing.TenantId));
        }

        var scopes = await query
            .Select(listing => new { listing.TenantId, listing.Marketplace })
            .Distinct()
            .ToListAsync(cancellationToken);

        return [.. scopes.Select(scope => new ListingSyncScope(scope.TenantId, scope.Marketplace))];
    }

    public async Task AddAsync(ProductListing listing, CancellationToken cancellationToken = default) =>
        await db.ProductListings.AddAsync(listing, cancellationToken);

    public async Task AddRangeAsync(
        IReadOnlyCollection<ProductListing> listings,
        CancellationToken cancellationToken = default) =>
        await db.ProductListings.AddRangeAsync(listings, cancellationToken);

    // listing zaten change tracker tarafından izleniyor.
    public void Update(ProductListing listing) => _ = listing;
}
