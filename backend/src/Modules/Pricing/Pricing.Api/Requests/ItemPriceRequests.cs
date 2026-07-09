using System.Text.Json.Serialization;

namespace Pricing.Api.Requests;

/// <summary>Kalem fiyatı oluşturma / güncelleme isteği.</summary>
public sealed record UpsertItemPriceRequest(
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string? Currency);
