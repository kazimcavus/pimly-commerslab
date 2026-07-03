using Channels.Domain.Connections;
using Channels.Domain.Marketplaces;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Channels.Infrastructure.Repositories;

/// <summary>MarketplaceConnection aggregate'leri için EF Core tabanlı depo.</summary>
internal sealed class MarketplaceConnectionRepository(ChannelsDbContext db) : IMarketplaceConnectionRepository
{
    public Task<MarketplaceConnection?> GetByMarketplaceAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default) =>
        db.MarketplaceConnections.FirstOrDefaultAsync(
            connection => connection.Marketplace == marketplace,
            cancellationToken);

    public async Task<IReadOnlySet<Marketplace>> GetConfiguredMarketplacesAsync(
        CancellationToken cancellationToken = default)
    {
        var keys = await db.MarketplaceConnections
            .Select(connection => connection.Marketplace)
            .ToListAsync(cancellationToken);

        return keys.ToHashSet();
    }

    public Task<MarketplaceConnection?> GetAnyEnabledAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default) =>
        db.MarketplaceConnections
            .IgnoreQueryFilters()
            .Where(connection => connection.Marketplace == marketplace && connection.IsEnabled)
            .OrderByDescending(connection => connection.ApiSecret != null)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(MarketplaceConnection connection, CancellationToken cancellationToken = default) =>
        await db.MarketplaceConnections.AddAsync(connection, cancellationToken);

    public void Update(MarketplaceConnection connection) =>
        db.MarketplaceConnections.Update(connection);
}
