using System.Text.Json;
using System.Text.Json.Serialization;
using Catalog.Domain.Products;
using Catalog.Domain.Variants;
using ProductVariant = Catalog.Domain.Products.Variant;
using ProductVariantValue = Catalog.Domain.Products.VariantValue;

namespace Catalog.Infrastructure.Persistence;

/// <summary>Product aggregate jsonb alanları için EF kalıcılık dönüşümleri.</summary>
internal static class ProductJsonPersistence
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    public static string SerializeAttributeValues(IReadOnlyList<AttributeValue> values) =>
        JsonSerializer.Serialize(values, SerializerOptions);

    public static List<AttributeValue> DeserializeAttributeValues(string json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<AttributeValue>>(json, SerializerOptions) ?? [];

    public static string SerializeVariants(IReadOnlyList<ProductVariant> variants) =>
        JsonSerializer.Serialize(variants, SerializerOptions);

    public static List<ProductVariant> DeserializeVariants(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var snapshots = JsonSerializer.Deserialize<List<VariantJson>>(json, SerializerOptions) ?? [];
        return snapshots
            .Select(snapshot => new ProductVariant(
                snapshot.Id,
                snapshot.Name,
                snapshot.SelectionStyle,
                snapshot.Slicer))
            .ToList();
    }

    public static string SerializeVariantValues(IReadOnlyList<ProductVariantValue> values) =>
        JsonSerializer.Serialize(values, SerializerOptions);

    public static List<ProductVariantValue> DeserializeVariantValues(string json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<ProductVariantValue>>(json, SerializerOptions) ?? [];

    private sealed record VariantJson(Guid Id, string Name, SelectionStyle SelectionStyle, bool Slicer = false);
}
