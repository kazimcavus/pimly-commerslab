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
    IReadOnlyList<CreateProductItemRequest> Items);

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
    IReadOnlyList<CreateProductItemRequest> Items);

/// <summary>Ürün güncelleme isteği.</summary>
public sealed record UpdateProductRequest(
    [property: JsonPropertyName("category_id")] Guid CategoryId,
    string Name,
    string Status,
    [property: JsonPropertyName("attribute_values")] JsonElement? AttributeValues);
