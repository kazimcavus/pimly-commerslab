using System.Text.Json.Serialization;

namespace Pricing.Api.Requests;

/// <summary>Kalem temel fiyatı oluşturma / güncelleme isteği.</summary>
public sealed record SetBasePriceRequest(
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("compare_at_amount")] decimal? CompareAtAmount,
    [property: JsonPropertyName("currency")] string? Currency);
