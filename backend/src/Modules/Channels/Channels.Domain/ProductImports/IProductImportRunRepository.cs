using SharedKernel;

namespace Channels.Domain.ProductImports;

/// <summary>
/// Pazaryerinden ürün içe aktarma job'larının (<see cref="ProductImportRun"/>) kalıcılık arabirimi.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> "Ürünlerimi çek" akışında oluşturulan import run kayıtlarını okumak, kuyruğa
/// eklemek ve worker'ın işlemesini sağlamak.</para>
/// <para><b>Kullananlar:</b> API (kuyruğa alma, durum sorgulama, geçmiş listeleme) ve arka plan
/// worker (<see cref="TryClaimNextPendingAsync"/> ile sıradaki işi alıp işleme).</para>
/// <para><b>Tasarım:</b> Import kuyruğu tenant'lar arası ortaktır; worker tenant bağlamı olmadan
/// claim eder, tenant bilgisi run üzerinde taşınır. Tenant'a özel sorgular <c>tenantId</c> parametresi
/// ile yapılır.</para>
/// </remarks>
public interface IProductImportRunRepository
{
    /// <summary>Run'ı kimliğe göre getirir; ürün bazlı hata kayıtları (<see cref="ProductImportRun.Errors"/>) dahil.</summary>
    Task<ProductImportRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenant'ın belirtilen pazaryerinde devam eden (pending veya running) import run'ını getirir.
    /// Eşzamanlı import engellemek için kuyruğa almadan önce kontrol edilir.
    /// </summary>
    Task<ProductImportRun?> GetActiveForTenantAsync(
        Guid tenantId,
        Marketplace marketplace,
        CancellationToken cancellationToken = default);

    /// <summary>Tenant'ın belirtilen pazaryerindeki son import run'larını yeniden eskiye sıralar.</summary>
    Task<IReadOnlyList<ProductImportRun>> ListRecentAsync(
        Guid tenantId,
        Marketplace marketplace,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kuyruktaki en eski pending run'ı PostgreSQL <c>FOR UPDATE SKIP LOCKED</c> ile kilitleyerek
    /// claim eder ve running durumuna alır. Eşzamanlı worker'lar aynı işi alamaz.
    /// Tenant bağlamı olmadan çağrılır; run yoksa <see langword="null"/> döner.
    /// <paramref name="tenantIds"/> doluysa yalnızca o tenant'ların run'ları claim edilir
    /// (tenant-izole worker instance'ları için); <see langword="null"/>/boş ise filtre uygulanmaz.
    /// </summary>
    Task<ProductImportRun?> TryClaimNextPendingAsync(
        IReadOnlyCollection<Guid>? tenantIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>Yeni pending import run kaydını ekler (kuyruğa alma).</summary>
    Task AddAsync(ProductImportRun run, CancellationToken cancellationToken = default);

    /// <summary>Run üzerindeki değişiklikleri (ilerleme, tamamlanma, hatalar) kalıcı hale getirmek için işaretler.</summary>
    void Update(ProductImportRun run);
}
