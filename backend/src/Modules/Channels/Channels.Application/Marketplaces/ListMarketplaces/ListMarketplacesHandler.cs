using Channels.Application.Contracts;
using Channels.Domain.Connections;
using SharedKernel;

namespace Channels.Application.Marketplaces.ListMarketplaces;

/// <summary>Pazaryeri listeleme işlemini yürüten handler.</summary>
public sealed class ListMarketplacesHandler(
    IMarketplaceConnectionRepository connections) : IListMarketplacesHandler
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<MarketplaceDto>>> ExecuteAsync(
        ListMarketplacesQuery query,
        CancellationToken cancellationToken = default)
    {
        _ = query;

        var configuredMarketplaces = await connections.GetConfiguredMarketplacesAsync(cancellationToken);

        var dtos = Marketplace.AllSupported
            .Select(marketplace => marketplace.ToDto(configuredMarketplaces.Contains(marketplace)))
            .ToList();

        return Result.Success<IReadOnlyList<MarketplaceDto>>(dtos);
    }
}
