using SharedKernel;

namespace Catalog.Domain.Variants;

/// <summary>
/// Bir varyant türüne ait seçilebilir değeri etiket, renk ve görsel bilgileriyle temsil eden varlık.
/// </summary>
/// <example>
/// "Renk" türü altında Label "Kırmızı", Color "#FF0000" olan bir varyant değeri.
/// </example>
public sealed class VariantValue : Entity<Guid>
{
    private VariantValue()
    {
    }

    internal VariantValue(
        Guid id,
        VariantKey key,
        string label,
        string? color,
        string? imageUrl,
        int sortOrder)
        : base(id)
    {
        Key = key;
        Label = label;
        Color = color;
        ImageUrl = imageUrl;
        SortOrder = sortOrder;
    }

    /// <summary>Gets değeri benzersiz tanımlayan anahtar; gönderilmezse etiketten türetilir.</summary>
    public VariantKey Key { get; private set; } = null!;

    /// <summary>Gets değerin görünen etiketi.</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>Gets görsel gösterim için renk; opsiyonel.</summary>
    public string? Color { get; private set; }

    /// <summary>Gets görsel gösterim için resim URL'si; opsiyonel.</summary>
    public string? ImageUrl { get; private set; }

    /// <summary>Gets tür içindeki görüntüleme sırası.</summary>
    public int SortOrder { get; private set; }

    internal void Update(
        VariantKey key,
        string label,
        string? color,
        string? imageUrl,
        int sortOrder)
    {
        Key = key;
        Label = label;
        Color = color;
        ImageUrl = imageUrl;
        SortOrder = sortOrder;
    }
}
