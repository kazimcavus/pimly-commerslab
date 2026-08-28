namespace Pimly.ProductImports.Worker.Options;

/// <summary>
/// Ürün import worker'ının çalışma ayarları. <see cref="TenantIds"/> boş bırakılırsa worker
/// tüm tenant'ların run'larını işler; tenant-izole instance çalıştırmak için liste doldurulur.
/// </summary>
public sealed class ProductImportsWorkerOptions
{
    /// <summary>Konfigürasyon bölümünün adı.</summary>
    public const string SectionName = "ProductImports";

    /// <summary>Gets kuyruk boşken iki claim denemesi arasındaki bekleme süresi (saniye).</summary>
    public int PollIntervalSeconds { get; init; } = 5;

    /// <summary>Gets bu worker instance'ının run'larını işleyeceği tenant'lar. Boş liste: tüm tenant'lar.</summary>
    public IReadOnlyList<Guid> TenantIds { get; init; } = [];
}
