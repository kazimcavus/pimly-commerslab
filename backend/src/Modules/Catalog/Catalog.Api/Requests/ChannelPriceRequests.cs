using System.Text.Json.Serialization;

namespace Catalog.Api.Requests;

/// <summary>Kalem kanal fiyatı oluşturma / güncelleme isteği.</summary>
public sealed record UpsertItemChannelPriceRequest(
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("compare_at_price")] decimal? CompareAtPrice,
    [property: JsonPropertyName("currency")] string? Currency);
