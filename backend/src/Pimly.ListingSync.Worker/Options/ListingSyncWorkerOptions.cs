namespace Pimly.ListingSync.Worker.Options;

/// <summary>
/// Listeleme (fiyat/stok) senkron worker'ının çalışma ayarları. <see cref="TenantIds"/> boş
/// bırakılırsa worker tüm tenant'ları senkronlar; tenant-izole instance için liste doldurulur.
/// </summary>
/// <remarks>
/// <see cref="PollIntervalSeconds"/> aynı zamanda debounce penceresidir: bir kalemin bu süre içindeki
/// tüm değişimleri tek gönderime iner. Büyütmek pazaryeri trafiğini azaltır, senkron gecikmesini artırır.
/// </remarks>
public sealed class ListingSyncWorkerOptions
{
    /// <summary>Konfigürasyon bölümünün adı.</summary>
    public const string SectionName = "ListingSync";

    /// <summary>Gets iki senkron turu arasındaki bekleme süresi (saniye).</summary>
    public int PollIntervalSeconds { get; init; } = 30;

    /// <summary>Gets bu worker instance'ının senkronlayacağı tenant'lar. Boş liste: tüm tenant'lar.</summary>
    public IReadOnlyList<Guid> TenantIds { get; init; } = [];
}
