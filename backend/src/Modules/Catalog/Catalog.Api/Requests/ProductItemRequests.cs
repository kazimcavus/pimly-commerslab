using System.Text.Json;
using System.Text.Json.Serialization;

namespace Catalog.Api.Requests;

/// <summary>Ürün kalemi oluşturma isteği.</summary>
public sealed record CreateProductItemRequest(
    string? Sku,
    string Barcode,
    string? Gtin,
    string? Mpn,
    [property: JsonPropertyName("axis_value_entry_id")] Guid? AxisValueEntryId,
    [property: JsonPropertyName("axis_value")] string? AxisValue,
    [property: JsonPropertyName("attribute_values")] JsonElement? AttributeValues,
    [property: JsonPropertyName("variant_values")] JsonElement? VariantValues);

/// <summary>Ürün kalemi güncelleme isteği.</summary>
/// <remarks>Sku/Barcode gönderilmezse mevcut değer korunur; boş sku metni SKU'yu temizler.</remarks>
public sealed record UpdateProductItemRequest(
    string? Gtin,
    string? Mpn,
    [property: JsonPropertyName("axis_value_entry_id")] Guid? AxisValueEntryId,
    [property: JsonPropertyName("axis_value")] string? AxisValue,
    [property: JsonPropertyName("attribute_values")] JsonElement? AttributeValues,
    string? Sku = null,
    string? Barcode = null);
