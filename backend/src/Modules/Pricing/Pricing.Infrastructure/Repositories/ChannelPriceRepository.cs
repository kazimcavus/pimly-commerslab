using Microsoft.EntityFrameworkCore;
using Pricing.Domain.ChannelPrices;
using Pricing.Infrastructure.Persistence;
using SharedKernel;

namespace Pricing.Infrastructure.Repositories;

/// <summary>ChannelPrice aggregate için veritabanı erişim katmanı.</summary>
internal sealed class ChannelPriceRepository(PricingDbContext db) : IChannelPriceRepository
{
    public async Task<ChannelPrice?> GetAsync(
        Guid productItemId,
        Marketplace marketplace,
        CancellationToken cancellationToken = default) =>
        await db.ChannelPrices
            .FirstOrDefaultAsync(
                p => p.ProductItemId == productItemId && p.Marketplace == marketplace,
                cancellationToken);

    public async Task<IReadOnlyList<ChannelPrice>> ListByItemAsync(
        Guid productItemId,
        CancellationToken cancellationToken = default) =>
        await db.ChannelPrices
            .Where(p => p.ProductItemId == productItemId)
            .OrderBy(p => p.Marketplace)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ChannelPrice channelPrice, CancellationToken cancellationToken = default) =>
        await db.ChannelPrices.AddAsync(channelPrice, cancellationToken);

    public void Update(ChannelPrice channelPrice) => db.ChannelPrices.Update(channelPrice);

    public void Remove(ChannelPrice channelPrice) => db.ChannelPrices.Remove(channelPrice);
}
