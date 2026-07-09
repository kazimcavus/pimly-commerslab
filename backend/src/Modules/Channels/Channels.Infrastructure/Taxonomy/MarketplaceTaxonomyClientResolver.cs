using Channels.Application.ExternalCatalog;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Channels.Infrastructure.Taxonomy;

/// <summary>Keyed DI ile pazaryeri taxonomy client çözümlemesi.</summary>
internal sealed class MarketplaceTaxonomyClientResolver(IServiceProvider serviceProvider)
    : IMarketplaceTaxonomyClientResolver
{
    /// <inheritdoc/>
    public Result<IMarketplaceTaxonomyClient> Resolve(Marketplace marketplace)
    {
        var client = serviceProvider.GetKeyedService<IMarketplaceTaxonomyClient>(marketplace.Code);
        return client is null
            ? Result.Failure<IMarketplaceTaxonomyClient>(
                Error.NotFound($"Taxonomy client is not configured for marketplace '{marketplace.Code}'."))
            : Result.Success(client);
    }
}
