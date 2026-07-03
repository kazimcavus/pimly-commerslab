using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.Marketplaces.ListMarketplaces;

/// <summary>Pazaryeri listeleme işlemini yürüten handler arabirimi.</summary>
public interface IListMarketplacesHandler
{
    Task<Result<IReadOnlyList<MarketplaceDto>>> ExecuteAsync(
        ListMarketplacesQuery query,
        CancellationToken cancellationToken = default);
}
