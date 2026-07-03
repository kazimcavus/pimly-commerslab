using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

/// <summary>ProductItemChannelPrice aggregate için veritabanı erişim katmanı.</summary>
internal sealed class ChannelPriceRepository(CatalogDbContext db) : IChannelPriceRepository
{
    public async Task<ProductItemChannelPrice?> GetAsync(
        Guid productItemId,
        string marketplaceKey,
        CancellationToken cancellationToken = default)
    {
        var key = marketplaceKey.Trim().ToLowerInvariant();
        return await db.Set<ProductItemChannelPrice>()
            .FirstOrDefaultAsync(
                p => p.ProductItemId == productItemId && p.MarketplaceKey == key,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProductItemChannelPrice>> ListByItemAsync(
        Guid productItemId,
        CancellationToken cancellationToken = default) =>
        await db.Set<ProductItemChannelPrice>()
            .Where(p => p.ProductItemId == productItemId)
            .OrderBy(p => p.MarketplaceKey)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductItemChannelPrice>> ListByProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var itemIds = db.ProductItems
            .Where(i => EF.Property<Guid>(i, "ProductId") == productId)
            .Select(i => i.Id);

        return await db.Set<ProductItemChannelPrice>()
            .Where(p => itemIds.Contains(p.ProductItemId))
            .OrderBy(p => p.ProductItemId)
            .ThenBy(p => p.MarketplaceKey)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProductItemChannelPrice channelPrice, CancellationToken cancellationToken = default) =>
        await db.Set<ProductItemChannelPrice>().AddAsync(channelPrice, cancellationToken);

    public void Update(ProductItemChannelPrice channelPrice) =>
        db.Set<ProductItemChannelPrice>().Update(channelPrice);

    public void Remove(ProductItemChannelPrice channelPrice) =>
        db.Set<ProductItemChannelPrice>().Remove(channelPrice);
}
