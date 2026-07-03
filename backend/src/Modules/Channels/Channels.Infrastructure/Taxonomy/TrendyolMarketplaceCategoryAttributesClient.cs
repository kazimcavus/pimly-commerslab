using Channels.Application.Taxonomy;
using Channels.Domain.Marketplaces;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Channels.Infrastructure.Taxonomy;

internal sealed class TrendyolMarketplaceCategoryAttributesClient(
    IHttpClientFactory httpClientFactory,
    ILogger<TrendyolMarketplaceCategoryAttributesClient> logger) : IMarketplaceCategoryAttributesClient
{
    public Task<Result<IReadOnlyList<MarketplaceCategoryAttributeNode>>> FetchCategoryAttributesAsync(
        MarketplaceDefinition marketplace,
        string externalCategoryId,
        CancellationToken cancellationToken = default)
    {
        _ = httpClientFactory;
        _ = marketplace;
        _ = externalCategoryId;
        cancellationToken.ThrowIfCancellationRequested();

        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                "Trendyol category attributes client is not fully implemented yet. Configure Channels:UseStubTaxonomyClient=true for development.");
        }

        return Task.FromResult(Result.Failure<IReadOnlyList<MarketplaceCategoryAttributeNode>>(
            Error.Failure("Trendyol category attributes API integration is not implemented yet.")));
    }
}
