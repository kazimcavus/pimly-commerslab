using Catalog.Domain.Variants.Events;
using SharedKernel;

namespace Catalog.Domain.Variants;

/// <summary>
/// Ürün varyasyon eksenlerini (renk, beden vb.) tanımlayan kök varlık.
/// SKU kombinasyonunu değil; eksen adını, seçim stilini, sıralamasını ve seçilebilir değerleri yönetir.
/// </summary>
/// <example>
/// "Renk" varyantı SelectionStyle Color, SortOrder 1, Slicer true ile oluşturulur;
/// "Kırmızı" (#FF0000) ve "Mavi" (#0000FF) değerleri eklenir. "Beden" varyantı List stiliyle ayrı tanımlanır.
/// </example>
public sealed class Variant : AggregateRoot<Guid>
{
    private readonly List<VariantValue> _values = [];

    private Variant()
    {
    }

    private Variant(Guid id, string name, SelectionStyle selectionStyle, int sortOrder, bool slicer)
        : base(id)
    {
        Name = name;
        SelectionStyle = selectionStyle;
        SortOrder = sortOrder;
        Slicer = slicer;
    }

    /// <summary>Gets varyant türünün adı.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets kullanıcı arayüzünde seçim stili.</summary>
    public SelectionStyle SelectionStyle { get; private set; }

    /// <summary>Gets ürün içindeki görüntüleme sırası.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Gets a value indicating whether filtreleme (slicer) olarak kullanılıp kullanılmadığı.</summary>
    public bool Slicer { get; private set; }

    /// <summary>Gets türe ait seçilebilir değerler.</summary>
    public IReadOnlyCollection<VariantValue> Values => _values.AsReadOnly();

    public static Result<Variant> Create(string name, SelectionStyle selectionStyle, int sortOrder, bool slicer = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Variant>(Error.Validation("Variant type name is required."));
        }

        var variant = new Variant(
            Guid.NewGuid(),
            name.Trim(),
            selectionStyle,
            sortOrder,
            slicer);

        variant.RaiseDomainEvent(new VariantCreated(variant.Id, variant.Name));
        return Result.Success(variant);
    }

    public Result Rename(string name, SelectionStyle selectionStyle, int sortOrder, bool slicer)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Variant type name is required."));
        }

        Name = name.Trim();
        SelectionStyle = selectionStyle;
        SortOrder = sortOrder;
        Slicer = slicer;
        return Result.Success();
    }

    public Result<VariantValue> AddValue(
        string label,
        string? color,
        string? imageUrl,
        string? code,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return Result.Failure<VariantValue>(Error.Validation("Variant value label is required."));
        }

        var trimmedLabel = label.Trim();
        if (_values.Any(v => string.Equals(v.Label, trimmedLabel, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<VariantValue>(
                Error.Conflict("Variant value label must be unique within the type."));
        }

        var value = new VariantValue(
            Guid.NewGuid(),
            trimmedLabel,
            string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            sortOrder);

        _values.Add(value);
        return Result.Success(value);
    }

    public Result UpdateValue(
        Guid valueId,
        string label,
        string? color,
        string? imageUrl,
        string? code,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return Result.Failure(Error.Validation("Variant value label is required."));
        }

        var value = _values.FirstOrDefault(v => v.Id == valueId);
        if (value is null)
        {
            return Result.Failure(Error.NotFound("Variant value not found."));
        }

        var trimmedLabel = label.Trim();
        if (_values.Any(v => v.Id != valueId &&
                             string.Equals(v.Label, trimmedLabel, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(Error.Conflict("Variant value label must be unique within the type."));
        }

        value.Update(
            trimmedLabel,
            string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            sortOrder);

        return Result.Success();
    }

    public Result RemoveValue(Guid valueId)
    {
        var value = _values.FirstOrDefault(v => v.Id == valueId);
        if (value is null)
        {
            return Result.Failure(Error.NotFound("Variant value not found."));
        }

        _values.Remove(value);
        return Result.Success();
    }

    internal void LoadValues(IEnumerable<VariantValue> values)
    {
        _values.Clear();
        _values.AddRange(values);
    }
}
