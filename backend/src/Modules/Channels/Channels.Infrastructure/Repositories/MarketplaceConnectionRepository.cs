using Channels.Domain.Connections;
using Channels.Domain.Marketplaces;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Channels.Infrastructure.Repositories;

/// <summary>MarketplaceConnection aggregate'leri için EF Core tabanlı depo.</summary>
internal sealed class MarketplaceConnectionRepository(ChannelsDbContext db) : IMarketplaceConnectionRepository
{
    public Task<MarketplaceConnection?> GetByMarketplaceKeyAsync(
        MarketplaceKey marketplaceKey,
        CancellationToken cancellationToken = default) =>
        db.MarketplaceConnections.FirstOrDefaultAsync(
            connection => connection.MarketplaceKey == marketplaceKey,
            cancellationToken);

    public async Task<IReadOnlySet<MarketplaceKey>> GetConfiguredMarketplaceKeysAsync(
        CancellationToken cancellationToken = default)
    {
        var keys = await db.MarketplaceConnections
            .Select(connection => connection.MarketplaceKey)
            .ToListAsync(cancellationToken);

        return keys.ToHashSet();
    }

    public async Task AddAsync(MarketplaceConnection connection, CancellationToken cancellationToken = default) =>
        await db.MarketplaceConnections.AddAsync(connection, cancellationToken);

    public void Update(MarketplaceConnection connection) =>
        db.MarketplaceConnections.Update(connection);
}
