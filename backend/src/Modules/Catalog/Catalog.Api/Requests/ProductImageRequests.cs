using System.Text.Json.Serialization;

namespace Catalog.Api.Requests;

/// <summary>Ürün galerisine görsel ekleme isteği.</summary>
internal sealed record AddProductImageRequest(
    string Url,
    [property: JsonPropertyName("sort_order")] int SortOrder,
    [property: JsonPropertyName("alt_text")] string? AltText,
    [property: JsonPropertyName("is_primary")] bool IsPrimary,
    [property: JsonPropertyName("variant_value_id")] Guid? VariantValueId);

/// <summary>Ürün galerisi görseli güncelleme isteği.</summary>
internal sealed record UpdateProductImageRequest(
    string Url,
    [property: JsonPropertyName("sort_order")] int SortOrder,
    [property: JsonPropertyName("alt_text")] string? AltText,
    [property: JsonPropertyName("is_primary")] bool IsPrimary,
    [property: JsonPropertyName("variant_value_id")] Guid? VariantValueId);
