using System.Text.Json.Serialization;
using Channels.Application.Connections;
using Channels.Application.Listings.ContentSync;
using Channels.Infrastructure.Options;
using Channels.Infrastructure.Taxonomy;
using Channels.Infrastructure.Trendyol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Channels.Infrastructure.Listings;

/// <summary>
/// Trendyol ürün kartı istemcisi. Yeni ürün için create ucuna, mevcut ürün için update ucuna gider;
/// her ikisi de bir batchRequestId döner ve onay asenkron ilerler.
/// </summary>
internal sealed class TrendyolMarketplaceListingClient(
    IHttpClientFactory httpClientFactory,
    IOptions<ChannelsOptions> options,
    ILogger<TrendyolMarketplaceListingClient> logger) : IMarketplaceListingClient
{
    /// <inheritdoc/>
    public int MaxBatchSize => 1000;

    /// <inheritdoc/>
    public async Task<Result<ListingSubmissionReceipt>> SubmitAsync(
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        IReadOnlyList<MarketplaceListingRequest> listings,
        bool isUpdate,
        CancellationToken cancellationToken = default)
    {
        _ = marketplace;

        if (string.IsNullOrWhiteSpace(credentials.SellerId))
        {
            return Result.Failure<ListingSubmissionReceipt>(
                Error.Validation("Seller id is required to submit Trendyol listings."));
        }

        if (listings.Count == 0)
        {
            return Result.Success(new ListingSubmissionReceipt(null));
        }

        var httpClient = httpClientFactory.CreateClient(nameof(TrendyolMarketplaceTaxonomyClient));
        var baseUrl = options.Value.TrendyolApiBaseUrl.TrimEnd('/');
        var sellerId = Uri.EscapeDataString(credentials.SellerId.Trim());

        var body = new TrendyolProductsRequest([.. listings.Select(ToTrendyolItem)]);
        var requestUri = $"{baseUrl}/integration/product/sellers/{sellerId}/products";

        var result = isUpdate
            ? await TrendyolHttpSupport.PutJsonAsync<TrendyolProductsRequest, TrendyolBatchResponse>(
                httpClient, requestUri, body, credentials, logger, cancellationToken)
            : await TrendyolHttpSupport.PostJsonAsync<TrendyolProductsRequest, TrendyolBatchResponse>(
                httpClient, requestUri, body, credentials, logger, cancellationToken);

        return result.IsFailure
            ? Result.Failure<ListingSubmissionReceipt>(result.Error)
            : Result.Success(new ListingSubmissionReceipt(result.Value.BatchRequestId));
    }

    private static TrendyolProductItem ToTrendyolItem(MarketplaceListingRequest listing) =>
        new(
            listing.Barcode,
            listing.Title,
            listing.ModelCode,
            listing.BrandName,
            ParseExternalId(listing.BrandExternalId),
            ParseExternalId(listing.ExternalCategoryId) ?? 0,
            listing.Quantity,
            listing.Sku ?? listing.Barcode,
            listing.Description,
            listing.CompareAtAmount ?? listing.Amount,
            listing.Amount,
            listing.Currency,
            [.. listing.ImageUrls.Select(url => new TrendyolProductImage(url))],
            [.. listing.Attributes.Select(attribute => new TrendyolProductAttribute(
                ParseExternalId(attribute.ExternalAttributeId) ?? 0,
                ParseExternalId(attribute.ExternalValueId),
                attribute.CustomValue))]);

    // Trendyol kategori/attribute kimlikleri sayısaldır; cache'te metin olarak tutulur.
    private static long? ParseExternalId(string? value) =>
        long.TryParse(value, out var parsed) ? parsed : null;

    private sealed record TrendyolProductsRequest(
        [property: JsonPropertyName("items")] IReadOnlyList<TrendyolProductItem> Items);

    private sealed record TrendyolProductItem(
        [property: JsonPropertyName("barcode")] string Barcode,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("productMainId")] string ProductMainId,
        [property: JsonPropertyName("brand")] string? Brand,
        [property: JsonPropertyName("brandId")] long? BrandId,
        [property: JsonPropertyName("categoryId")] long CategoryId,
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("stockCode")] string StockCode,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("listPrice")] decimal ListPrice,
        [property: JsonPropertyName("salePrice")] decimal SalePrice,
        [property: JsonPropertyName("currencyType")] string CurrencyType,
        [property: JsonPropertyName("images")] IReadOnlyList<TrendyolProductImage> Images,
        [property: JsonPropertyName("attributes")] IReadOnlyList<TrendyolProductAttribute> Attributes);

    private sealed record TrendyolProductImage(
        [property: JsonPropertyName("url")] string Url);

    private sealed record TrendyolProductAttribute(
        [property: JsonPropertyName("attributeId")] long AttributeId,
        [property: JsonPropertyName("attributeValueId")] long? AttributeValueId,
        [property: JsonPropertyName("customAttributeValue")] string? CustomAttributeValue);

    private sealed record TrendyolBatchResponse(
        [property: JsonPropertyName("batchRequestId")] string? BatchRequestId);
}
