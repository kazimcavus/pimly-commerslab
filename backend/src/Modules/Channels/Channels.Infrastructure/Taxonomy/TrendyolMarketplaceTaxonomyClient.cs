using Channels.Application.Taxonomy;
using Channels.Domain.Marketplaces;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Channels.Infrastructure.Taxonomy;

/// <summary>Trendyol kategori API istemcisi; gerçek endpoint entegrasyonu için iskelet.</summary>
internal sealed class TrendyolMarketplaceTaxonomyClient(
    IHttpClientFactory httpClientFactory,
    ILogger<TrendyolMarketplaceTaxonomyClient> logger) : IMarketplaceTaxonomyClient
{
    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<MarketplaceCategoryNode>>> FetchAllCategoriesAsync(
        MarketplaceDefinition marketplace,
        CancellationToken cancellationToken = default)
    {
        _ = httpClientFactory;
        _ = marketplace;
        cancellationToken.ThrowIfCancellationRequested();

        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                "Trendyol taxonomy client is not fully implemented yet. Configure Channels:UseStubTaxonomyClient=true for development.");
        }

        return Task.FromResult(Result.Failure<IReadOnlyList<MarketplaceCategoryNode>>(
            Error.Failure("Trendyol taxonomy API integration is not implemented yet.")));
    }
}
