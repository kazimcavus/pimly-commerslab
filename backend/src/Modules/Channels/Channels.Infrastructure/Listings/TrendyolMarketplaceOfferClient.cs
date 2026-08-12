using System.Text.Json.Serialization;
using Channels.Application.Connections;
using Channels.Application.Listings.OfferSync;
using Channels.Infrastructure.Options;
using Channels.Infrastructure.Taxonomy;
using Channels.Infrastructure.Trendyol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Channels.Infrastructure.Listings;

/// <summary>
/// Trendyol fiyat/stok güncelleme istemcisi (price-and-inventory). Ürün kartını değiştirmediği için
/// yeniden onay tetiklemez ve tek çağrıda çok sayıda kalem kabul eder.
/// </summary>
internal sealed class TrendyolMarketplaceOfferClient(
    IHttpClientFactory httpClientFactory,
    IOptions<ChannelsOptions> options,
    ILogger<TrendyolMarketplaceOfferClient> logger) : IMarketplaceOfferClient
{
    /// <inheritdoc/>
    public int MaxBatchSize => 1000;

    /// <inheritdoc/>
    public async Task<Result<OfferUpdateReceipt>> UpdateOffersAsync(
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        IReadOnlyList<MarketplaceOfferUpdate> offers,
        CancellationToken cancellationToken = default)
    {
        _ = marketplace;

        if (string.IsNullOrWhiteSpace(credentials.SellerId))
        {
            return Result.Failure<OfferUpdateReceipt>(
                Error.Validation("Seller id is required to update Trendyol offers."));
        }

        if (offers.Count == 0)
        {
            return Result.Success(new OfferUpdateReceipt(null));
        }

        var httpClient = httpClientFactory.CreateClient(nameof(TrendyolMarketplaceTaxonomyClient));
        var baseUrl = options.Value.TrendyolApiBaseUrl.TrimEnd('/');
        var sellerId = Uri.EscapeDataString(credentials.SellerId.Trim());

        var body = new TrendyolPriceInventoryRequest(
            [.. offers.Select(offer => new TrendyolPriceInventoryItem(
                offer.ExternalListingId,
                offer.Quantity,
                offer.Amount,
                offer.CompareAtAmount ?? offer.Amount))]);

        var result = await TrendyolHttpSupport.PostJsonAsync<TrendyolPriceInventoryRequest, TrendyolBatchResponse>(
            httpClient,
            $"{baseUrl}/integration/inventory/sellers/{sellerId}/products/price-and-inventory",
            body,
            credentials,
            logger,
            cancellationToken);

        return result.IsFailure
            ? Result.Failure<OfferUpdateReceipt>(result.Error)
            : Result.Success(new OfferUpdateReceipt(result.Value.BatchRequestId));
    }

    private sealed record TrendyolPriceInventoryRequest(
        [property: JsonPropertyName("items")] IReadOnlyList<TrendyolPriceInventoryItem> Items);

    private sealed record TrendyolPriceInventoryItem(
        [property: JsonPropertyName("barcode")] string Barcode,
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("salePrice")] decimal SalePrice,
        [property: JsonPropertyName("listPrice")] decimal ListPrice);

    private sealed record TrendyolBatchResponse(
        [property: JsonPropertyName("batchRequestId")] string? BatchRequestId);
}
