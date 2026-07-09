using SharedKernel;

namespace Channels.Domain.Connections;

/// <summary>Pazaryeri bağlantılarının kalıcılık işlemlerini tanımlayan depo arabirimi.</summary>
public interface IMarketplaceConnectionRepository
{
    Task<MarketplaceConnection?> GetByMarketplaceAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Marketplace>> GetConfiguredMarketplacesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pazaryeri için etkin herhangi bir bağlantıyı tenant filtresinden bağımsız getirir.
    /// Taksonomi pazaryeri-global olduğundan kategori/attribute çekiminde hangi tenant'ın
    /// kimlik bilgisinin kullanıldığı önemsizdir.
    /// </summary>
    Task<MarketplaceConnection?> GetAnyEnabledAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default);

    Task AddAsync(MarketplaceConnection connection, CancellationToken cancellationToken = default);

    void Update(MarketplaceConnection connection);
}
