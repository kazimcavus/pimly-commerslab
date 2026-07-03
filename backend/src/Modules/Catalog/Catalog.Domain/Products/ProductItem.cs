using SharedKernel;

namespace Catalog.Domain.Products;

/// <summary>
/// Ürün altında satılabilir SKU/barkod birimini temsil eden varlık.
/// Fiyat, stok ve eksen/özellik değerlerini taşır.
/// </summary>
/// <example>
/// Renk: Kırmızı, Beden: S seçimleriyle Barcode "8690000001", Price 299.99, Stock 10.
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
        decimal price,
        decimal? compareAtPrice,
        int stock,
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
        Price = price;
        CompareAtPrice = compareAtPrice;
        Stock = stock;
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

    /// <summary>Gets satış fiyatı.</summary>
    /// <example>299.99.</example>
    public decimal Price { get; private set; }

    /// <summary>Gets karşılaştırma veya indirim öncesi fiyat; opsiyonel.</summary>
    /// <example>349.99.</example>
    public decimal? CompareAtPrice { get; private set; }

    /// <summary>Gets stok miktarı.</summary>
    /// <example>10.</example>
    public int Stock { get; private set; }

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

        if (draft.Price < 0)
        {
            return Result.Failure<ProductItem>(Error.Validation("Variant price cannot be negative."));
        }

        if (draft.Stock < 0)
        {
            return Result.Failure<ProductItem>(Error.Validation("Variant stock cannot be negative."));
        }

        return Result.Success(new ProductItem(
            Guid.NewGuid(),
            string.IsNullOrWhiteSpace(draft.Sku) ? null : draft.Sku.Trim(),
            draft.Barcode.Trim(),
            NullIfWhiteSpace(draft.Gtin),
            NullIfWhiteSpace(draft.Mpn),
            draft.AxisValueEntryId,
            NullIfWhiteSpace(draft.AxisValue),
            draft.Price,
            draft.CompareAtPrice,
            draft.Stock,
            draft.AttributeValues ?? [],
            draft.VariantValues ?? []));
    }

    /// <summary>Kalemin fiyat, stok, tanımlayıcı ve özellik bilgilerini günceller.</summary>
    /// <param name="update">Güncelleme girdisi.</param>
    internal Result Update(ProductItemUpdate update)
    {
        if (update.Price < 0)
        {
            return Result.Failure(Error.Validation("Variant price cannot be negative."));
        }

        if (update.Stock < 0)
        {
            return Result.Failure(Error.Validation("Variant stock cannot be negative."));
        }

        Gtin = NullIfWhiteSpace(update.Gtin);
        Mpn = NullIfWhiteSpace(update.Mpn);
        AxisValueEntryId = update.AxisValueEntryId;
        AxisValue = NullIfWhiteSpace(update.AxisValue);
        Price = update.Price;
        CompareAtPrice = update.CompareAtPrice;
        Stock = update.Stock;
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
/// <example>Barcode "8690000001", Renk/Beden seçimleri ve fiyat/stok bilgisi.</example>
public sealed record ProductItemDraft(
    string? Sku,
    string Barcode,
    string? Gtin,
    string? Mpn,
    Guid? AxisValueEntryId,
    string? AxisValue,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    IReadOnlyList<AttributeValue>? AttributeValues,
    IReadOnlyList<VariantValue>? VariantValues);

/// <summary>Mevcut satılabilir kalem güncelleme girdisi.</summary>
/// <example>Price 279.99, Stock 8 ve güncellenmiş özellik değerleri.</example>
public sealed record ProductItemUpdate(
    string? Gtin,
    string? Mpn,
    Guid? AxisValueEntryId,
    string? AxisValue,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    IReadOnlyList<AttributeValue>? AttributeValues);
