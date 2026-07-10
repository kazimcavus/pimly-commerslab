using SharedKernel;

namespace Channels.Domain.Publications;

/// <summary>
/// Ürün yayın job'larının (<see cref="ProductPublicationRun"/>) kalıcılık arabirimi. ProductImportRun
/// deposunun outbound aynasıdır: kuyruk tenant'lar arası ortaktır, worker tenant bağlamı olmadan claim eder.
/// </summary>
public interface IProductPublicationRunRepository
{
    /// <summary>Run'ı kimliğe göre getirir; kalem bazlı hata kayıtları dahil.</summary>
    Task<ProductPublicationRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Tenant'ın belirtilen pazaryerinde devam eden (pending veya running) run'ını getirir.</summary>
    Task<ProductPublicationRun?> GetActiveForTenantAsync(
        Guid tenantId,
        Marketplace marketplace,
        CancellationToken cancellationToken = default);

    /// <summary>Tenant'ın belirtilen pazaryerindeki son run'larını yeniden eskiye sıralar.</summary>
    Task<IReadOnlyList<ProductPublicationRun>> ListRecentAsync(
        Guid tenantId,
        Marketplace marketplace,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kuyruktaki en eski pending run'ı <c>FOR UPDATE SKIP LOCKED</c> ile claim edip running yapar.
    /// Tenant bağlamı olmadan çağrılır; run yoksa null döner.
    /// </summary>
    Task<ProductPublicationRun?> TryClaimNextPendingAsync(
        IReadOnlyCollection<Guid>? tenantIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>Yeni pending run kaydını ekler (kuyruğa alma).</summary>
    Task AddAsync(ProductPublicationRun run, CancellationToken cancellationToken = default);

    /// <summary>Run üzerindeki değişiklikleri kalıcı hale getirmek için işaretler.</summary>
    void Update(ProductPublicationRun run);
}
