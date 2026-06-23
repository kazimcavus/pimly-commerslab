namespace Catalog.Domain.SkuGenerator;

/// <summary>SKU şablonundaki tek bir segment tanımı.</summary>
public sealed class SkuSegment
{
    public string Type { get; init; } = string.Empty;

    public string? Label { get; init; }

    public string? Value { get; init; }

    public int? Start { get; init; }

    public int? Width { get; init; }

    public int? Digits { get; init; }

    public string? Source { get; init; }

    public bool IsVariantSegment =>
        string.Equals(Type, SkuSegmentTypes.Color, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Type, SkuSegmentTypes.Size, StringComparison.OrdinalIgnoreCase);

    public bool IsCounterSegment =>
        string.Equals(Type, SkuSegmentTypes.Counter, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Desteklenen SKU segment tipleri.</summary>
public static class SkuSegmentTypes
{
    public const string Fixed = "fixed";
    public const string Manual = "manual";
    public const string Counter = "counter";
    public const string Year = "year";
    public const string Color = "color";
    public const string Size = "size";
}
