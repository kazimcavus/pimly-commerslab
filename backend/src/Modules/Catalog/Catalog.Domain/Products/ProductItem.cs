using SharedKernel;

namespace Catalog.Domain.Products;

/// <summary>
/// Ürün altında satılabilir SKU/barkod birimini temsil eden varlık.
/// Kimlik ve eksen/özellik değerlerini taşır. Fiyat Pricing'e, stok Inventory'ye taşınmıştır.
/// </summary>
/// <example>
/// Renk: Kırmızı, Beden: S seçimleriyle Barcode "8690000001".
/// </example>
public sealed class ProductItem : Entity<Guid>
{
    private ProductItem()
    {
    }

    internal ProductItem(
        Guid id,
        string? sku,
        string barcode,
        string? gtin,
        string? mpn,
        Guid? axisValueEntryId,
        string? axisValue,
        IReadOnlyList<AttributeValue> attributeValues,
        IReadOnlyList<VariantValue> variantValues)
        : base(id)
    {
        Sku = sku;
        Barcode = barcode;
        Gtin = gtin;
        Mpn = mpn;
        AxisValueEntryId = axisValueEntryId;
        AxisValue = axisValue;
        AttributeValues = attributeValues;
        VariantValues = variantValues;
    }

    /// <summary>Gets varyant SKU'su; opsiyonel.</summary>
    /// <example>GOMlek-001-KIRMIZI-S.</example>
    public string? Sku { get; private set; }

    /// <summary>Gets varyant barkodu.</summary>
    /// <example>8690000001.</example>
    public string Barcode { get; private set; } = string.Empty;

    /// <summary>Gets global Ticari Ürün Numarası; opsiyonel.</summary>
    /// <example>8690123456789.</example>
    public string? Gtin { get; private set; }

    /// <summary>Gets üretici Parça Numarası; opsiyonel.</summary>
    /// <example>MP-12345.</example>
    public string? Mpn { get; private set; }

    /// <summary>Gets tek eksenli varyantlarda seçilen değer girişinin tanımlayıcısı.</summary>
    /// <example>Renk katalog değerinin Guid'i.</example>
    public Guid? AxisValueEntryId { get; private set; }

    /// <summary>Gets tek eksenli varyantlarda serbest metin eksen değeri.</summary>
    /// <example>Özel baskı metni.</example>
    public string? AxisValue { get; private set; }

    /// <summary>Gets varyant düzeyinde özellik değerleri.</summary>
    /// <example>Bu kalemde "Desen" özelliğinde "Çizgili" değeri.</example>
    public IReadOnlyList<AttributeValue> AttributeValues { get; private set; } = [];

    /// <summary>Gets varyantın temsil ettiği eksen değerleri.</summary>
    /// <example>Renk: Kırmızı, Beden: S anlık görüntüleri.</example>
    public IReadOnlyList<VariantValue> VariantValues { get; private set; } = [];

    /// <summary>Taslak girdiden yeni satılabilir kalem oluşturur.</summary>
    /// <param name="draft">Kalem oluşturma girdisi.</param>
    internal static Result<ProductItem> Create(ProductItemDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Barcode))
        {
            return Result.Failure<ProductItem>(Error.Validation("Variant barcode is required."));
        }

        return Result.Success(new ProductItem(
            Guid.NewGuid(),
            string.IsNullOrWhiteSpace(draft.Sku) ? null : draft.Sku.Trim(),
            draft.Barcode.Trim(),
            NullIfWhiteSpace(draft.Gtin),
            NullIfWhiteSpace(draft.Mpn),
            draft.AxisValueEntryId,
            NullIfWhiteSpace(draft.AxisValue),
            draft.AttributeValues ?? [],
            draft.VariantValues ?? []));
    }

    /// <summary>Kalemin tanımlayıcı ve özellik bilgilerini günceller.</summary>
    /// <param name="update">Güncelleme girdisi.</param>
    /// <remarks>Barcode null ise mevcut barkod korunur; Sku null ise mevcut SKU korunur (boş metin SKU'yu temizler).</remarks>
    internal Result Update(ProductItemUpdate update)
    {
        if (update.Barcode is not null)
        {
            if (string.IsNullOrWhiteSpace(update.Barcode))
            {
                return Result.Failure(Error.Validation("Variant barcode is required."));
            }

            Barcode = update.Barcode.Trim();
        }

        if (update.Sku is not null)
        {
            Sku = NullIfWhiteSpace(update.Sku);
        }

        Gtin = NullIfWhiteSpace(update.Gtin);
        Mpn = NullIfWhiteSpace(update.Mpn);
        AxisValueEntryId = update.AxisValueEntryId;
        AxisValue = NullIfWhiteSpace(update.AxisValue);
        if (update.AttributeValues is not null)
        {
            AttributeValues = update.AttributeValues;
        }

        return Result.Success();
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Yeni satılabilir kalem oluşturma girdisi.</summary>
/// <example>Barcode "8690000001", Renk/Beden seçimleri.</example>
public sealed record ProductItemDraft(
    string? Sku,
    string Barcode,
    string? Gtin,
    string? Mpn,
    Guid? AxisValueEntryId,
    string? AxisValue,
    IReadOnlyList<AttributeValue>? AttributeValues,
    IReadOnlyList<VariantValue>? VariantValues);

/// <summary>Mevcut satılabilir kalem güncelleme girdisi.</summary>
/// <example>Güncellenmiş özellik değerleri.</example>
/// <remarks>Barcode/Sku null bırakılırsa mevcut değer korunur; boş Sku metni SKU'yu temizler.</remarks>
public sealed record ProductItemUpdate(
    string? Gtin,
    string? Mpn,
    Guid? AxisValueEntryId,
    string? AxisValue,
    IReadOnlyList<AttributeValue>? AttributeValues,
    string? Sku = null,
    string? Barcode = null);
