namespace Channels.Domain.Listings;

/// <summary>Bir kalemin pazaryerindeki listeleme yaşam döngüsü durumu.</summary>
/// <remarks>
/// <see cref="Channels.Domain.Publications.PublicationStatus"/> bir <em>job</em> durumudur (geçmiş);
/// bu enum ise <em>ilişkinin</em> güncel durumudur ve run'lardan bağımsız yaşar.
/// </remarks>
public enum ListingStatus
{
    /// <summary>Kayıt açıldı, pazaryerine hiç gönderilmedi.</summary>
    Pending,

    /// <summary>Gönderildi; pazaryerinin onayı/işlemesi bekleniyor.</summary>
    Submitted,

    /// <summary>Pazaryerinde aktif olarak listeleniyor.</summary>
    Live,

    /// <summary>Pazaryeri içeriği reddetti; düzeltilip yeniden gönderilmeli.</summary>
    Rejected,

    /// <summary>Yayından kaldırılması istendi, henüz kaldırılmadı.</summary>
    PendingDelist,

    /// <summary>Pazaryerinden kaldırıldı.</summary>
    Delisted,
}
