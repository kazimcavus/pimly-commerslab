using System.Text.Json;
using System.Text.Json.Serialization;
using Catalog.Application.Products;
using Catalog.Domain.Products;
using Catalog.Domain.Variants;
using ProductVariant = Catalog.Domain.Products.Variant;

namespace Catalog.Api;

/// <summary>HTTP isteklerindeki JSON alanlarını domain tiplerine dönüştürür.</summary>
internal static class ProductInputMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    internal static IReadOnlyList<AttributeValueInput> MapAttributeValues(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<AttributeValueInputJson>>(
            element.Value.GetRawText(),
            SerializerOptions)?
            .Select(input => new AttributeValueInput(
                input.AttributeId,
                input.AttributeValueId))
            .ToList() ?? [];
    }

    internal static IReadOnlyList<ProductVariant> MapVariants(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        var snapshots = JsonSerializer.Deserialize<List<VariantJson>>(
            element.Value.GetRawText(),
            SerializerOptions) ?? [];

        return snapshots
            .Select(snapshot => new ProductVariant(
                snapshot.Id,
                snapshot.Name,
                Enum.Parse<SelectionStyle>(snapshot.SelectionStyle, true),
                snapshot.Slicer))
            .ToList();
    }

    internal static IReadOnlyList<VariantValueInput> MapVariantValues(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<VariantValueInputJson>>(
            element.Value.GetRawText(),
            SerializerOptions)?
            .Select(input => new VariantValueInput(
                input.VariantId,
                input.VariantValueId))
            .ToList() ?? [];
    }

    private sealed record AttributeValueInputJson(
        [property: JsonPropertyName("attribute_id")] Guid AttributeId,
        [property: JsonPropertyName("attribute_value_id")] Guid AttributeValueId);

    private sealed record VariantValueInputJson(
        [property: JsonPropertyName("variant_id")] Guid VariantId,
        [property: JsonPropertyName("variant_value_id")] Guid VariantValueId);

    private sealed record VariantJson(Guid Id, string Name, string SelectionStyle, bool Slicer = false);
}
