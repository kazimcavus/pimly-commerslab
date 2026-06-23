using System.Text.Json;
using Catalog.Domain.SkuGenerator;

namespace Catalog.Infrastructure.Persistence;

/// <summary>SkuGeneratorConfig jsonb alanları için EF kalıcılık dönüşümleri.</summary>
internal static class SkuGeneratorJsonPersistence
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string SerializeSegments(IReadOnlyList<SkuSegment> segments) =>
        JsonSerializer.Serialize(segments, SerializerOptions);

    public static List<SkuSegment> DeserializeSegments(string json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<SkuSegment>>(json, SerializerOptions) ?? [];
}
