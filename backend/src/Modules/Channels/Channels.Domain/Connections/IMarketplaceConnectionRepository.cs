using Channels.Domain.Marketplaces;

namespace Channels.Domain.Connections;

/// <summary>Pazaryeri bağlantılarının kalıcılık işlemlerini tanımlayan depo arabirimi.</summary>
public interface IMarketplaceConnectionRepository
{
    Task<MarketplaceConnection?> GetByMarketplaceAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Marketplace>> GetConfiguredMarketplacesAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(MarketplaceConnection connection, CancellationToken cancellationToken = default);

    void Update(MarketplaceConnection connection);
}
