namespace Catalog.Domain.SkuGenerator;

/// <summary>SKU şablonundaki tek bir segment tanımı.</summary>
public sealed class SkuSegment
{
    /// <summary>Gets segment tipi; fixed, manual, counter, year, color veya size.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Gets manual segmentler için kullanıcı arayüzü etiketi; opsiyonel.</summary>
    public string? Label { get; init; }

    /// <summary>Gets fixed segmentler için sabit token değeri; opsiyonel.</summary>
    public string? Value { get; init; }

    /// <summary>Gets counter segmentler için başlangıç değeri; opsiyonel.</summary>
    public int? Start { get; init; }

    /// <summary>Gets counter token genişliği; opsiyonel.</summary>
    public int? Width { get; init; }

    /// <summary>Gets year segmentlerinde kullanılacak basamak sayısı (2 veya 4); opsiyonel.</summary>
    public int? Digits { get; init; }

    /// <summary>Gets color/size segmentlerinde varyant değerinden alınacak alan; key, name veya code.</summary>
    public string? Source { get; init; }

    /// <summary>Gets a value indicating whether segment color veya size varyant token'ı üretir.</summary>
    public bool IsVariantSegment =>
        string.Equals(Type, SkuSegmentTypes.Color, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Type, SkuSegmentTypes.Size, StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether segment artan sayaç token'ı üretir.</summary>
    public bool IsCounterSegment =>
        string.Equals(Type, SkuSegmentTypes.Counter, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Desteklenen SKU segment tipleri.</summary>
public static class SkuSegmentTypes
{
    /// <summary>Sabit metin token'ı.</summary>
    public const string Fixed = "fixed";

    /// <summary>Kullanıcı girdisiyle doldurulan token.</summary>
    public const string Manual = "manual";

    /// <summary>Artan sayaç token'ı.</summary>
    public const string Counter = "counter";

    /// <summary>Yıl token'ı.</summary>
    public const string Year = "year";

    /// <summary>Renk varyant değerinden türetilen token.</summary>
    public const string Color = "color";

    /// <summary>Beden ve diğer list varyant değerlerinden türetilen token.</summary>
    public const string Size = "size";
}
