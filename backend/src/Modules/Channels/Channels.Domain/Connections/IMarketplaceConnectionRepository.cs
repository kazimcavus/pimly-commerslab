using Channels.Domain.Marketplaces;

namespace Channels.Domain.Connections;

/// <summary>Pazaryeri bağlantılarının kalıcılık işlemlerini tanımlayan depo arabirimi.</summary>
public interface IMarketplaceConnectionRepository
{
    Task<MarketplaceConnection?> GetByMarketplaceKeyAsync(
        MarketplaceKey marketplaceKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<MarketplaceKey>> GetConfiguredMarketplaceKeysAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(MarketplaceConnection connection, CancellationToken cancellationToken = default);

    void Update(MarketplaceConnection connection);
}
