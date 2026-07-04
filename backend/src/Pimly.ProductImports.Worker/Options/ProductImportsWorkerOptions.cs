namespace Pimly.ProductImports.Worker.Options;

/// <summary>
/// Ürün import worker'ının çalışma ayarları. Her worker instance'ı hangi tenant'lara
/// hizmet ettiğini <see cref="TenantIds"/> ile açıkça bildirmek zorundadır; liste boşsa
/// worker startup'ta doğrulama hatasıyla durur (sessizce "tüm tenant'lar" moduna düşülmez).
/// </summary>
public sealed class ProductImportsWorkerOptions
{
    /// <summary>Konfigürasyon bölümünün adı.</summary>
    public const string SectionName = "ProductImports";

    /// <summary>Gets kuyruk boşken iki claim denemesi arasındaki bekleme süresi (saniye).</summary>
    public int PollIntervalSeconds { get; init; } = 5;

    /// <summary>Gets bu worker instance'ının run'larını işleyeceği tenant'lar. Zorunlu; boş olamaz.</summary>
    public IReadOnlyList<Guid> TenantIds { get; init; } = [];
}
