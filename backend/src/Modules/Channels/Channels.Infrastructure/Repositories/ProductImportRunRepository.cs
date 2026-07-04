using Channels.Domain.Marketplaces;
using Channels.Domain.ProductImports;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Channels.Infrastructure.Repositories;

/// <summary>ProductImportRun aggregate'leri için EF Core tabanlı depo.</summary>
/// <remarks>
/// Worker tenant bağlamı olmadan çalıştığı için sorgular tenant'ı açık parametre olarak alır;
/// run aggregate'i tenant query filter listesinde DEĞİLDİR (kuyruk tenant'lar arası ortaktır).
/// </remarks>
internal sealed class ProductImportRunRepository(ChannelsDbContext db, TimeProvider timeProvider)
    : IProductImportRunRepository
{
    public Task<ProductImportRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.ProductImportRuns
            .Include(run => run.Errors)
            .FirstOrDefaultAsync(run => run.Id == id, cancellationToken);

    public Task<ProductImportRun?> GetActiveForTenantAsync(
        Guid tenantId,
        Marketplace marketplace,
        CancellationToken cancellationToken = default) =>
        db.ProductImportRuns
            .Where(run =>
                run.TenantId == tenantId
                && run.Marketplace == marketplace
                && (run.Status == ProductImportStatus.Pending || run.Status == ProductImportStatus.Running))
            .OrderByDescending(run => run.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductImportRun>> ListRecentAsync(
        Guid tenantId,
        Marketplace marketplace,
        int limit,
        CancellationToken cancellationToken = default) =>
        await db.ProductImportRuns
            .Where(run => run.TenantId == tenantId && run.Marketplace == marketplace)
            .OrderByDescending(run => run.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<ProductImportRun?> TryClaimNextPendingAsync(
        IReadOnlyCollection<Guid>? tenantIds = null,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Tenant-izole worker instance'ları yalnızca kendi tenant'larının run'larını claim eder;
        // filtre yoksa kuyruk tenant'lar arası ortak davranır. Npgsql, Guid[] parametresini uuid[]e map eder.
        var tenantFilter = tenantIds is { Count: > 0 } ? tenantIds.ToArray() : null;

        var pendingQuery = tenantFilter is null
            ? db.ProductImportRuns
                .FromSqlInterpolated($"""
                    SELECT id, tenant_id, marketplace_code, status, created_at, started_at, completed_at,
                           total_products, processed_products, imported_products, skipped_products,
                           failed_products, error_message
                    FROM channels.product_import_runs
                    WHERE status = {"pending"}
                    ORDER BY created_at
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                    """)
            : db.ProductImportRuns
                .FromSqlInterpolated($"""
                    SELECT id, tenant_id, marketplace_code, status, created_at, started_at, completed_at,
                           total_products, processed_products, imported_products, skipped_products,
                           failed_products, error_message
                    FROM channels.product_import_runs
                    WHERE status = {"pending"} AND tenant_id = ANY({tenantFilter})
                    ORDER BY created_at
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                    """);

        var pendingId = await pendingQuery
            .Select(run => run.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (pendingId == Guid.Empty)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var run = await db.ProductImportRuns
            .Include(candidate => candidate.Errors)
            .FirstAsync(candidate => candidate.Id == pendingId, cancellationToken);

        var markResult = run.MarkRunning(timeProvider.GetUtcNow());
        if (markResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return run;
    }

    public async Task AddAsync(ProductImportRun run, CancellationToken cancellationToken = default) =>
        await db.ProductImportRuns.AddAsync(run, cancellationToken);

    // run zaten izleniyor (GetByIdAsync). Ekstra Update gerekmez; yeni hata child'larının
    // durumu ChannelsDbContext.SaveChangesAsync içinde düzeltilir (append-only → Added).
    public void Update(ProductImportRun run) => _ = run;
}
