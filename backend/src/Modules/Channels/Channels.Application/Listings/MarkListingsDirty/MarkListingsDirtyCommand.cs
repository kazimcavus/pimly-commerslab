namespace Channels.Application.Listings.MarkListingsDirty;

/// <summary>
/// Bir kalemin listelemelerini "gönderilmeyi bekliyor" olarak işaretleme komutu. Uydu modüllerden
/// (Catalog, Pricing, Inventory) gelen değişim sinyallerinin Channels tarafındaki karşılığıdır.
/// </summary>
/// <param name="ProductItemId">Değişen kalemin kimliği.</param>
/// <param name="MarketplaceCode">
/// Yalnızca belirli bir pazaryerini işaretlemek için kod; null ise kalemin tüm listelemeleri işaretlenir.
/// </param>
/// <param name="Kind">İşaretlenecek değişim sınıfı.</param>
public sealed record MarkListingsDirtyCommand(
    Guid ProductItemId,
    string? MarketplaceCode,
    ListingDirtyKind Kind);

/// <summary>Kirlilik sınıfı — pazaryerinde farklı uçlara ve maliyetlere karşılık gelir.</summary>
public enum ListingDirtyKind
{
    /// <summary>Fiyat/stok değişimi: ucuz, toplu ve yeniden onay gerektirmeyen uç.</summary>
    Offer,

    /// <summary>İçerik değişimi: pahalı ve pazaryerinde yeniden onaya giren uç.</summary>
    Content,

    /// <summary>Hem içerik hem fiyat/stok değişti.</summary>
    Both,
}
