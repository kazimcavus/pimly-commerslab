using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.Connections.UpsertMarketplaceConnection;

/// <summary>Pazaryeri bağlantısı upsert işlemini yürüten handler arabirimi.</summary>
public interface IUpsertMarketplaceConnectionHandler
{
    Task<Result<MarketplaceConnectionDto>> ExecuteAsync(
        UpsertMarketplaceConnectionCommand command,
        CancellationToken cancellationToken = default);
}
