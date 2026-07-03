using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.Connections.GetMarketplaceConnection;

/// <summary>Pazaryeri bağlantısı getirme işlemini yürüten handler arabirimi.</summary>
public interface IGetMarketplaceConnectionHandler
{
    Task<Result<MarketplaceConnectionDto>> ExecuteAsync(
        GetMarketplaceConnectionQuery query,
        CancellationToken cancellationToken = default);
}
