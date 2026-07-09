using Channels.Application.ExternalCatalog;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Channels.Infrastructure.Taxonomy;

/// <summary>Keyed DI ile pazaryeri kategori attribute client çözümlemesi.</summary>
internal sealed class MarketplaceCategoryAttributesClientResolver(IServiceProvider serviceProvider)
    : IMarketplaceCategoryAttributesClientResolver
{
    /// <inheritdoc/>
    public Result<IMarketplaceCategoryAttributesClient> Resolve(Marketplace marketplace)
    {
        var client = serviceProvider.GetKeyedService<IMarketplaceCategoryAttributesClient>(marketplace.Code);
        return client is null
            ? Result.Failure<IMarketplaceCategoryAttributesClient>(
                Error.NotFound($"Category attributes client is not configured for marketplace '{marketplace.Code}'."))
            : Result.Success(client);
    }
}
