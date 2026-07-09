using System.Text.Json.Serialization;

namespace Pricing.Api.Requests;

/// <summary>Kanal (pazaryeri) fiyatı oluşturma / güncelleme isteği.</summary>
public sealed record SetChannelPriceRequest(
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("compare_at_amount")] decimal? CompareAtAmount,
    [property: JsonPropertyName("currency")] string? Currency);
