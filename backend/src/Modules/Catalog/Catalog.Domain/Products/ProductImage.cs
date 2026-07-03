using SharedKernel;

namespace Catalog.Domain.Products;

/// <summary>Ürün galerisindeki tek bir görseli temsil eden varlık.</summary>
public sealed class ProductImage : Entity<Guid>
{
    private ProductImage()
    {
    }

    internal ProductImage(
        Guid id,
        string url,
        int sortOrder,
        string? altText,
        bool isPrimary,
        Guid? variantValueId)
        : base(id)
    {
        Url = url;
        SortOrder = sortOrder;
        AltText = altText;
        IsPrimary = isPrimary;
        VariantValueId = variantValueId;
    }

    /// <summary>Gets görselin erişilebilir URL'si.</summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>Gets galeri içindeki sıralama.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Gets erişilebilirlik metni; opsiyonel.</summary>
    public string? AltText { get; private set; }

    /// <summary>Gets a value indicating whether kapak görseli olup olmadığı.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Gets ilişkili varyant değeri; opsiyonel.</summary>
    public Guid? VariantValueId { get; private set; }

    /// <summary>Taslak girdiden yeni galeri görseli oluşturur.</summary>
    /// <param name="url">Görsel URL'si.</param>
    /// <param name="sortOrder">Galeri sıralaması.</param>
    /// <param name="altText">Erişilebilirlik metni; opsiyonel.</param>
    /// <param name="isPrimary">Kapak görseli olarak işaretlensin mi.</param>
    /// <param name="variantValueId">İlişkili varyant değeri; opsiyonel.</param>
    internal static Result<ProductImage> Create(
        string url,
        int sortOrder,
        string? altText,
        bool isPrimary,
        Guid? variantValueId)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result.Failure<ProductImage>(Error.Validation("Image URL is required."));
        }

        return Result.Success(new ProductImage(
            Guid.NewGuid(),
            url.Trim(),
            sortOrder,
            NullIfWhiteSpace(altText),
            isPrimary,
            variantValueId));
    }

    /// <summary>Görsel URL'si, sıralama, erişilebilirlik ve birincil bayrağını günceller.</summary>
    /// <param name="url">Yeni görsel URL'si.</param>
    /// <param name="sortOrder">Yeni galeri sıralaması.</param>
    /// <param name="altText">Yeni erişilebilirlik metni; opsiyonel.</param>
    /// <param name="isPrimary">Kapak görseli olarak işaretlensin mi.</param>
    /// <param name="variantValueId">İlişkili varyant değeri; opsiyonel.</param>
    internal Result Update(
        string url,
        int sortOrder,
        string? altText,
        bool isPrimary,
        Guid? variantValueId)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result.Failure(Error.Validation("Image URL is required."));
        }

        Url = url.Trim();
        SortOrder = sortOrder;
        AltText = NullIfWhiteSpace(altText);
        IsPrimary = isPrimary;
        VariantValueId = variantValueId;

        return Result.Success();
    }

    /// <summary>Görselin kapak görseli olup olmadığını ayarlar.</summary>
    /// <param name="isPrimary">Kapak görseli bayrağı.</param>
    internal void MarkPrimary(bool isPrimary) => IsPrimary = isPrimary;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
