namespace Channels.Application.Listings.ContentSync;

/// <summary>
/// Catalog modülünden, pazaryerine gönderilecek ürün içeriğini okuyan ACL portu. Channels, Catalog
/// tiplerine doğrudan bağımlanmaz; implementasyon composition root'ta kurulur.
/// </summary>
public interface ICatalogListingSourceGateway
{
    /// <summary>Verilen kalemlerin içerik anlık görüntülerini toplu okur; bulunamayan kalem sonuçta yer almaz.</summary>
    /// <param name="productItemIds">Okunacak kalem kimlikleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kalem başına içerik anlık görüntüsü.</returns>
    Task<IReadOnlyList<CatalogListingSource>> GetAsync(
        IReadOnlyCollection<Guid> productItemIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen kategorilerdeki tüm satılabilir kalemlerin kimliklerini listeler. Yayın, yalnızca
    /// pazaryeri kategorisine eşlenmiş kategorilerdeki kalemleri kapsar.
    /// </summary>
    /// <param name="categoryIds">Kapsanacak Catalog kategori kimlikleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kalem kimlikleri.</returns>
    Task<IReadOnlyList<Guid>> ListItemIdsByCategoriesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken = default);
}

/// <summary>Bir satılabilir kalemin pazaryerine giden kanonik içeriği.</summary>
/// <param name="ProductItemId">Kalem kimliği.</param>
/// <param name="ProductId">Bağlı ürün kimliği.</param>
/// <param name="CategoryId">Ürünün Catalog kategorisi (pazaryeri kategorisine eşlenecek).</param>
/// <param name="Title">Listeleme başlığı.</param>
/// <param name="Description">Opsiyonel açıklama (HTML olabilir).</param>
/// <param name="BrandName">Opsiyonel marka adı.</param>
/// <param name="BrandExternalCode">Markanın pazaryerindeki kimliği (import sırasında marka koduna yazılır).</param>
/// <param name="ModelCode">Ürün grubu kodu — pazaryerinde varyantları aynı karta bağlar.</param>
/// <param name="Barcode">Kalemin barkodu; pazaryerindeki listeleme kimliğidir.</param>
/// <param name="Sku">Opsiyonel stok kodu.</param>
/// <param name="Attributes">Kalem üzerinde geçerli özellik ve varyant seçimleri.</param>
/// <param name="ImageUrls">Galeri görselleri, sıralı.</param>
public sealed record CatalogListingSource(
    Guid ProductItemId,
    Guid ProductId,
    Guid CategoryId,
    string Title,
    string? Description,
    string? BrandName,
    string? BrandExternalCode,
    string ModelCode,
    string Barcode,
    string? Sku,
    IReadOnlyList<CatalogListingSelection> Attributes,
    IReadOnlyList<string> ImageUrls);

/// <summary>
/// Kalem üzerindeki tek bir özellik veya varyant seçimi. Kaynak tipi, Channels'ın doğru eşleme
/// tablosuna bakabilmesi için taşınır.
/// </summary>
/// <param name="IsVariant">true ise varyant ekseni, false ise özellik.</param>
/// <param name="SourceId">Catalog'daki özellik veya varyant ekseni kimliği.</param>
/// <param name="ValueId">Catalog'daki değer kimliği.</param>
/// <param name="ValueLabel">Değerin okunabilir karşılığı; eşleme bulunamazsa serbest metin olarak gönderilir.</param>
public sealed record CatalogListingSelection(
    bool IsVariant,
    Guid SourceId,
    Guid ValueId,
    string ValueLabel);
