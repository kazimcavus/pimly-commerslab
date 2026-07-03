using Channels.Application.ExternalCatalog;
using Channels.Domain.Connections;
using Channels.Domain.Marketplaces;
using Channels.Infrastructure.Options;
using Channels.Infrastructure.Trendyol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Channels.Infrastructure.Taxonomy;

/// <summary>
/// Trendyol kategori ağacı istemcisi. getCategoryTree endpoint'inden tüm ağacı çekip
/// düz <see cref="MarketplaceCategoryNode"/> listesine açar. Kimlik bilgisi gerektiğinde
/// herhangi bir etkin bağlantıdan çözer (taksonomi pazaryeri-globaldir).
/// </summary>
internal sealed class TrendyolMarketplaceTaxonomyClient(
    IHttpClientFactory httpClientFactory,
    IMarketplaceConnectionRepository connections,
    IOptions<ChannelsOptions> options,
    ILogger<TrendyolMarketplaceTaxonomyClient> logger) : IMarketplaceTaxonomyClient
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<MarketplaceCategoryNode>>> FetchAllCategoriesAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(TrendyolMarketplaceTaxonomyClient));
        var baseUrl = options.Value.TrendyolApiBaseUrl.TrimEnd('/');
        var credentials = await TrendyolHttpSupport.ResolveAnyEnabledCredentialsAsync(
            connections,
            marketplace,
            cancellationToken);

        var fetchResult = await TrendyolHttpSupport.GetJsonAsync<TrendyolCategoryTreeResponse>(
            httpClient,
            $"{baseUrl}/integration/product/product-categories",
            credentials,
            logger,
            cancellationToken);

        if (fetchResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<MarketplaceCategoryNode>>(fetchResult.Error);
        }

        var nodes = new List<MarketplaceCategoryNode>();
        Flatten(fetchResult.Value.Categories ?? [], parentExternalId: null, parentPath: null, nodes);

        return Result.Success<IReadOnlyList<MarketplaceCategoryNode>>(nodes);
    }

    private static void Flatten(
        IReadOnlyList<TrendyolCategoryNode> categories,
        string? parentExternalId,
        string? parentPath,
        List<MarketplaceCategoryNode> target)
    {
        foreach (var category in categories)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                continue;
            }

            var externalId = category.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var path = parentPath is null ? category.Name : $"{parentPath} > {category.Name}";
            var children = category.SubCategories ?? [];

            target.Add(new MarketplaceCategoryNode(
                externalId,
                category.Name,
                parentExternalId,
                path,
                IsLeaf: children.Count == 0));

            Flatten(children, externalId, path, target);
        }
    }

    private sealed record TrendyolCategoryTreeResponse(IReadOnlyList<TrendyolCategoryNode>? Categories);

    private sealed record TrendyolCategoryNode(
        long Id,
        string? Name,
        long? ParentId,
        IReadOnlyList<TrendyolCategoryNode>? SubCategories);
}
