using Channels.Domain.Marketplaces;

namespace Channels.Domain.Imports;

/// <summary>ProductImportRun aggregate depo arabirimi.</summary>
public interface IProductImportRunRepository
{
    /// <summary>Kimliğe göre run getirir (hatalarıyla birlikte).</summary>
    Task<ProductImportRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Tenant'ın belirtilen pazaryerindeki aktif (pending/running) run'ını getirir.</summary>
    Task<ProductImportRun?> GetActiveForTenantAsync(
        Guid tenantId,
        Marketplace marketplace,
        CancellationToken cancellationToken = default);

    /// <summary>Tenant'ın son run'larını yeniden eskiye listeler.</summary>
    Task<IReadOnlyList<ProductImportRun>> ListRecentAsync(
        Guid tenantId,
        Marketplace marketplace,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sıradaki pending run'ı kilitleyerek (FOR UPDATE SKIP LOCKED) claim eder ve running durumuna alır.
    /// Worker tarafından tenant bağlamı olmadan çağrılır.
    /// </summary>
    Task<ProductImportRun?> TryClaimNextPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Yeni run ekler.</summary>
    Task AddAsync(ProductImportRun run, CancellationToken cancellationToken = default);

    /// <summary>Run'ı günceller.</summary>
    void Update(ProductImportRun run);
}
