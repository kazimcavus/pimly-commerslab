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
/// Trendyol kategori attribute istemcisi. getCategoryAttributes endpoint'inden kategori
/// özelliklerini çeker; varianter → IsVariant, slicer → IsSlicer olarak eşlenir.
/// </summary>
internal sealed class TrendyolMarketplaceCategoryAttributesClient(
    IHttpClientFactory httpClientFactory,
    IMarketplaceConnectionRepository connections,
    IOptions<ChannelsOptions> options,
    ILogger<TrendyolMarketplaceCategoryAttributesClient> logger) : IMarketplaceCategoryAttributesClient
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<MarketplaceCategoryAttributeNode>>> FetchCategoryAttributesAsync(
        Marketplace marketplace,
        string externalCategoryId,
        CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(TrendyolMarketplaceTaxonomyClient));
        var baseUrl = options.Value.TrendyolApiBaseUrl.TrimEnd('/');
        var credentials = await TrendyolHttpSupport.ResolveAnyEnabledCredentialsAsync(
            connections,
            marketplace,
            cancellationToken);

        var fetchResult = await TrendyolHttpSupport.GetJsonAsync<TrendyolCategoryAttributesResponse>(
            httpClient,
            $"{baseUrl}/integration/product/product-categories/{Uri.EscapeDataString(externalCategoryId.Trim())}/attributes",
            credentials,
            logger,
            cancellationToken);

        if (fetchResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<MarketplaceCategoryAttributeNode>>(fetchResult.Error);
        }

        IReadOnlyList<MarketplaceCategoryAttributeNode> nodes = (fetchResult.Value.CategoryAttributes ?? [])
            .Where(attribute => attribute.Attribute is not null
                && !string.IsNullOrWhiteSpace(attribute.Attribute.Name))
            .Select(attribute => new MarketplaceCategoryAttributeNode(
                attribute.Attribute!.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                attribute.Attribute.Name!,
                attribute.Required,
                attribute.AllowCustom,
                attribute.Varianter,
                (attribute.AttributeValues ?? [])
                    .Where(value => !string.IsNullOrWhiteSpace(value.Name))
                    .Select(value => new MarketplaceAttributeValueNode(
                        value.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        value.Name!))
                    .ToList(),
                IsSlicer: attribute.Slicer))
            .ToList();

        return Result.Success(nodes);
    }

    private sealed record TrendyolCategoryAttributesResponse(
        IReadOnlyList<TrendyolCategoryAttribute>? CategoryAttributes);

    private sealed record TrendyolCategoryAttribute(
        TrendyolAttributeRef? Attribute,
        bool Required,
        bool AllowCustom,
        bool Varianter,
        bool Slicer,
        IReadOnlyList<TrendyolAttributeValue>? AttributeValues);

    private sealed record TrendyolAttributeRef(long Id, string? Name);

    private sealed record TrendyolAttributeValue(long Id, string? Name);
}
