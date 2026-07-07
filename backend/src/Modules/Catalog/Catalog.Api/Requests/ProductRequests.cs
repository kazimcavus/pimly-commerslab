using System.Text.Json;
using System.Text.Json.Serialization;

namespace Catalog.Api.Requests;

/// <summary>Ürün oluşturma isteği.</summary>
public sealed record CreateProductRequest(
    [property: JsonPropertyName("group_id")] Guid GroupId,
    [property: JsonPropertyName("category_id")] Guid CategoryId,
    [property: JsonPropertyName("model_code")] string ModelCode,
    string Name,
    string Status,
    [property: JsonPropertyName("code_inputs")] IReadOnlyList<string>? CodeInputs,
    [property: JsonPropertyName("attribute_values")] JsonElement? AttributeValues,
    [property: JsonPropertyName("variants")] JsonElement? Variants,
    IReadOnlyList<CreateProductItemRequest> Items,
    [property: JsonPropertyName("brand_id")] Guid? BrandId = null,
    [property: JsonPropertyName("description")] string? Description = null);

/// <summary>Toplu ürün oluşturma isteği.</summary>
public sealed record CreateProductsBatchRequest(
    [property: JsonPropertyName("group_id")] Guid GroupId,
    [property: JsonPropertyName("products")] IReadOnlyList<BatchProductRequest> Products);

/// <summary>Toplu oluşturma isteğindeki tek ürün girdisi.</summary>
public sealed record BatchProductRequest(
    [property: JsonPropertyName("category_id")] Guid CategoryId,
    [property: JsonPropertyName("model_code")] string ModelCode,
    string Name,
    string Status,
    [property: JsonPropertyName("code_inputs")] IReadOnlyList<string>? CodeInputs,
    [property: JsonPropertyName("attribute_values")] JsonElement? AttributeValues,
    [property: JsonPropertyName("variants")] JsonElement? Variants,
    IReadOnlyList<CreateProductItemRequest> Items,
    [property: JsonPropertyName("brand_id")] Guid? BrandId = null,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("splits")] IReadOnlyList<ProductSplitRequest>? Splits = null);

/// <summary>Slicer değeri başına model kodu/ad geçersiz kılma girdisi.</summary>
/// <remarks>ValueName boş olan girdiler bölme sırasında yok sayılır (import davranışıyla uyumlu).</remarks>
public sealed record ProductSplitRequest(
    [property: JsonPropertyName("value_name")] string ValueName,
    [property: JsonPropertyName("model_code")] string? ModelCode = null,
    string? Name = null,
    string? Description = null);

/// <summary>Ürün güncelleme isteği.</summary>
public sealed record UpdateProductRequest(
    [property: JsonPropertyName("category_id")] Guid CategoryId,
    string Name,
    string Status,
    [property: JsonPropertyName("attribute_values")] JsonElement? AttributeValues,
    [property: JsonPropertyName("brand_id")] Guid? BrandId = null,
    [property: JsonPropertyName("description")] string? Description = null);
