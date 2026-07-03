using Catalog.Domain.Variants.Events;
using SharedKernel;

namespace Catalog.Domain.Variants;

/// <summary>
/// Ürün varyasyon eksenlerini (renk, beden vb.) tanımlayan kök varlık.
/// SKU kombinasyonunu değil; eksen adını, seçim stilini, sıralamasını ve seçilebilir değerleri yönetir.
/// </summary>
/// <example>
/// "Renk" varyantı key değeri RENK, SelectionStyle Color, SortOrder 1, Slicer true ile oluşturulur;
/// "Kırmızı" (#FF0000) ve "Mavi" (#0000FF) değerleri eklenir. "Beden" varyantı List stiliyle ayrı tanımlanır.
/// </example>
public sealed class Variant : AggregateRoot<Guid>
{
    private readonly List<VariantValue> _values = [];

    private Variant()
    {
    }

    private Variant(Guid id, VariantKey key, string name, SelectionStyle selectionStyle, int sortOrder, bool slicer)
        : base(id)
    {
        Key = key;
        Name = name;
        SelectionStyle = selectionStyle;
        SortOrder = sortOrder;
        Slicer = slicer;
    }

    /// <summary>Gets varyant türünü benzersiz tanımlayan anahtar; oluşturulurken adından türetilir.</summary>
    /// <example>RENK.</example>
    public VariantKey Key { get; private set; } = null!;

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

    /// <summary>Yeni varyant türü oluşturur ve <see cref="VariantCreated"/> alan olayını yayımlar.</summary>
    /// <param name="name">Varyant türü adı.</param>
    /// <param name="selectionStyle">Kullanıcı arayüzü seçim stili.</param>
    /// <param name="sortOrder">Görüntüleme sırası.</param>
    /// <param name="slicer">Filtreleme (slicer) olarak kullanılsın mı.</param>
    /// <param name="key">Açık anahtar; boş bırakılırsa adından türetilir.</param>
    public static Result<Variant> Create(
        string name,
        SelectionStyle selectionStyle,
        int sortOrder,
        bool slicer = false,
        string? key = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Variant>(Error.Validation("Variant type name is required."));
        }

        var trimmedName = name.Trim();
        var keyResult = VariantKey.FromOptional(key, trimmedName);
        if (keyResult.IsFailure)
        {
            return Result.Failure<Variant>(keyResult.Error);
        }

        var variant = new Variant(
            Guid.NewGuid(),
            keyResult.Value,
            trimmedName,
            selectionStyle,
            sortOrder,
            slicer);

        variant.RaiseDomainEvent(new VariantCreated(variant.Id, variant.Key.Value));
        return Result.Success(variant);
    }

    /// <summary>Varyant türünün adını, seçim stilini, sırasını ve slicer bayrağını günceller.</summary>
    /// <param name="name">Yeni tür adı.</param>
    /// <param name="selectionStyle">Yeni seçim stili.</param>
    /// <param name="sortOrder">Yeni görüntüleme sırası.</param>
    /// <param name="slicer">Filtreleme (slicer) olarak kullanılsın mı.</param>
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

    /// <summary>Varyant türüne yeni seçilebilir değer ekler.</summary>
    /// <param name="label">Değer etiketi.</param>
    /// <param name="color">Görsel renk kodu; opsiyonel.</param>
    /// <param name="imageUrl">Görsel URL'si; opsiyonel.</param>
    /// <param name="key">Değer anahtarı; boş bırakılırsa etiketten türetilir.</param>
    /// <param name="sortOrder">Tür içindeki görüntüleme sırası.</param>
    public Result<VariantValue> AddValue(
        string label,
        string? color,
        string? imageUrl,
        string? key,
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

        var keyResult = VariantKey.FromOptional(key, trimmedLabel);
        if (keyResult.IsFailure)
        {
            return Result.Failure<VariantValue>(keyResult.Error);
        }

        if (_values.Any(v => string.Equals(v.Key.Value, keyResult.Value.Value, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<VariantValue>(
                Error.Conflict("Variant value key must be unique within the type."));
        }

        var value = new VariantValue(
            Guid.NewGuid(),
            keyResult.Value,
            trimmedLabel,
            string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            sortOrder);

        _values.Add(value);
        return Result.Success(value);
    }

    /// <summary>Mevcut varyant değerinin etiket, görsel ve anahtar bilgilerini günceller.</summary>
    /// <param name="valueId">Güncellenecek değer tanımlayıcısı.</param>
    /// <param name="label">Yeni değer etiketi.</param>
    /// <param name="color">Yeni renk kodu; opsiyonel.</param>
    /// <param name="imageUrl">Yeni görsel URL'si; opsiyonel.</param>
    /// <param name="key">Yeni değer anahtarı; boş bırakılırsa etiketten türetilir.</param>
    /// <param name="sortOrder">Yeni görüntüleme sırası.</param>
    public Result UpdateValue(
        Guid valueId,
        string label,
        string? color,
        string? imageUrl,
        string? key,
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

        var keyResult = VariantKey.FromOptional(key, trimmedLabel);
        if (keyResult.IsFailure)
        {
            return Result.Failure(keyResult.Error);
        }

        if (_values.Any(v => v.Id != valueId &&
                             string.Equals(v.Key.Value, keyResult.Value.Value, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(Error.Conflict("Variant value key must be unique within the type."));
        }

        value.Update(
            keyResult.Value,
            trimmedLabel,
            string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            sortOrder);

        return Result.Success();
    }

    /// <summary>Varyant türünden bir değeri kaldırır.</summary>
    /// <param name="valueId">Kaldırılacak değer tanımlayıcısı.</param>
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

    /// <summary>Kalıcı depodan yüklenen değerleri aggregate'e bağlar.</summary>
    /// <param name="values">Türe ait değer koleksiyonu.</param>
    internal void LoadValues(IEnumerable<VariantValue> values)
    {
        _values.Clear();
        _values.AddRange(values);
    }
}
