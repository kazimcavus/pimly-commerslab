using Channels.Application.Connections;
using Channels.Application.Imports;
using Channels.Domain.Marketplaces;
using Channels.Infrastructure.Options;
using Channels.Infrastructure.Taxonomy;
using Channels.Infrastructure.Trendyol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Channels.Infrastructure.Imports;

/// <summary>
/// Trendyol satıcı ürünleri istemcisi (filterProducts). Sayfalı ürün listesini çeker;
/// varyantlar productMainId ile gruplu, attribute'lar düz listedir.
/// </summary>
internal sealed class TrendyolMarketplaceProductsClient(
    IHttpClientFactory httpClientFactory,
    IOptions<ChannelsOptions> options,
    ILogger<TrendyolMarketplaceProductsClient> logger) : IMarketplaceProductsClient
{
    /// <inheritdoc/>
    public async Task<Result<MarketplaceProductPage>> FetchProductsPageAsync(
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        int page,
        int size,
        CancellationToken cancellationToken = default)
    {
        _ = marketplace;
        if (string.IsNullOrWhiteSpace(credentials.SellerId))
        {
            return Result.Failure<MarketplaceProductPage>(
                Error.Validation("Seller id is required to fetch Trendyol products."));
        }

        var httpClient = httpClientFactory.CreateClient(nameof(TrendyolMarketplaceTaxonomyClient));
        var baseUrl = options.Value.TrendyolApiBaseUrl.TrimEnd('/');
        var sellerId = Uri.EscapeDataString(credentials.SellerId.Trim());

        var fetchResult = await TrendyolHttpSupport.GetJsonAsync<TrendyolProductsResponse>(
            httpClient,
            $"{baseUrl}/integration/product/sellers/{sellerId}/products?page={page}&size={size}&approved=true",
            credentials,
            logger,
            cancellationToken);

        if (fetchResult.IsFailure)
        {
            return Result.Failure<MarketplaceProductPage>(fetchResult.Error);
        }

        var response = fetchResult.Value;
        IReadOnlyList<MarketplaceProductNode> items = (response.Content ?? [])
            .Where(product => !string.IsNullOrWhiteSpace(product.Barcode)
                && !string.IsNullOrWhiteSpace(product.Title))
            .Select(product => new MarketplaceProductNode(
                product.Barcode!.Trim(),
                product.Title!.Trim(),
                string.IsNullOrWhiteSpace(product.ProductMainId) ? product.Barcode!.Trim() : product.ProductMainId.Trim(),
                product.Brand,
                product.StockCode,
                product.Quantity,
                product.ListPrice,
                product.SalePrice,
                product.CurrencyType,
                product.PimCategoryId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                product.CategoryName,
                product.Description,
                product.Approved,
                (product.Images ?? [])
                    .Select(image => image.Url)
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select(url => url!.Trim())
                    .ToList(),
                (product.Attributes ?? [])
                    .Where(attribute => attribute.AttributeId is not null)
                    .Select(attribute => new MarketplaceProductAttributeNode(
                        attribute.AttributeId!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        attribute.AttributeName ?? string.Empty,
                        attribute.AttributeValueId?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        attribute.AttributeValue,
                        attribute.CustomAttributeValue))
                    .ToList()))
            .ToList();

        return Result.Success(new MarketplaceProductPage(
            response.TotalElements,
            response.TotalPages,
            response.Page,
            response.Size,
            items));
    }

    private sealed record TrendyolProductsResponse(
        long TotalElements,
        int TotalPages,
        int Page,
        int Size,
        IReadOnlyList<TrendyolProduct>? Content);

    private sealed record TrendyolProduct(
        string? Barcode,
        string? Title,
        string? ProductMainId,
        string? Brand,
        string? StockCode,
        int Quantity,
        decimal ListPrice,
        decimal SalePrice,
        string? CurrencyType,
        long? PimCategoryId,
        string? CategoryName,
        string? Description,
        bool Approved,
        IReadOnlyList<TrendyolProductImage>? Images,
        IReadOnlyList<TrendyolProductAttribute>? Attributes);

    private sealed record TrendyolProductImage(string? Url);

    private sealed record TrendyolProductAttribute(
        long? AttributeId,
        string? AttributeName,
        long? AttributeValueId,
        string? AttributeValue,
        string? CustomAttributeValue);
}
