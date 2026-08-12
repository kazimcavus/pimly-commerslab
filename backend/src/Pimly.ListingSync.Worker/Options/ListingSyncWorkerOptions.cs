namespace Pimly.ListingSync.Worker.Options;

/// <summary>
/// Listeleme (fiyat/stok) senkron worker'ının çalışma ayarları. Her worker instance'ı hangi
/// tenant'lara hizmet ettiğini <see cref="TenantIds"/> ile açıkça bildirmek zorundadır; liste boşsa
/// worker startup'ta doğrulama hatasıyla durur.
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

    /// <summary>Gets bu worker instance'ının senkronlayacağı tenant'lar. Zorunlu; boş olamaz.</summary>
    public IReadOnlyList<Guid> TenantIds { get; init; } = [];
}
