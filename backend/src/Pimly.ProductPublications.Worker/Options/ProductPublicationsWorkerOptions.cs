namespace Pimly.ProductPublications.Worker.Options;

/// <summary>
/// Ürün yayın (publish) worker'ının çalışma ayarları. Her worker instance'ı hangi tenant'lara
/// hizmet ettiğini <see cref="TenantIds"/> ile açıkça bildirmek zorundadır; liste boşsa worker
/// startup'ta doğrulama hatasıyla durur (sessizce "tüm tenant'lar" moduna düşülmez).
/// </summary>
public sealed class ProductPublicationsWorkerOptions
{
    /// <summary>Konfigürasyon bölümünün adı.</summary>
    public const string SectionName = "ProductPublications";

    /// <summary>Gets kuyruk boşken iki claim denemesi arasındaki bekleme süresi (saniye).</summary>
    public int PollIntervalSeconds { get; init; } = 5;

    /// <summary>Gets bu worker instance'ının run'larını işleyeceği tenant'lar. Zorunlu; boş olamaz.</summary>
    public IReadOnlyList<Guid> TenantIds { get; init; } = [];
}
