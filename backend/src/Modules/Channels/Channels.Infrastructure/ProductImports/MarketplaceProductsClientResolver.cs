using Channels.Application.ProductImports;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Channels.Infrastructure.ProductImports;

/// <summary>Keyed DI ile pazaryeri ürün client çözümlemesi.</summary>
internal sealed class MarketplaceProductsClientResolver(IServiceProvider serviceProvider)
    : IMarketplaceProductsClientResolver
{
    /// <inheritdoc/>
    public Result<IMarketplaceProductsClient> Resolve(Marketplace marketplace)
    {
        var client = serviceProvider.GetKeyedService<IMarketplaceProductsClient>(marketplace.Code);
        return client is null
            ? Result.Failure<IMarketplaceProductsClient>(
                Error.NotFound($"Products client is not configured for marketplace '{marketplace.Code}'."))
            : Result.Success(client);
    }
}
