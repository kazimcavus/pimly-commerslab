using Channels.Application.Contracts;
using Channels.Domain.Connections;
using Channels.Domain.Marketplaces;
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

        var configuredKeys = await connections.GetConfiguredMarketplaceKeysAsync(cancellationToken);

        var dtos = MarketplaceRegistry.ListActive()
            .Select(marketplace => marketplace.ToDto(configuredKeys.Contains(marketplace.Key)))
            .ToList();

        return Result.Success<IReadOnlyList<MarketplaceDto>>(dtos);
    }
}
