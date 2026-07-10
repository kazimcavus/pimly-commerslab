using Channels.Domain.Publications;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Channels.Infrastructure.Repositories;

/// <summary>ProductPublicationRun aggregate'leri için EF Core tabanlı depo.</summary>
/// <remarks>
/// Worker tenant bağlamı olmadan çalıştığı için sorgular tenant'ı açık parametre alır; run aggregate'i
/// tenant query filter listesinde DEĞİLDİR (kuyruk tenant'lar arası ortaktır).
/// </remarks>
internal sealed class ProductPublicationRunRepository(ChannelsDbContext db, TimeProvider timeProvider)
    : IProductPublicationRunRepository
{
    public Task<ProductPublicationRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.ProductPublicationRuns
            .Include(run => run.Errors)
            .FirstOrDefaultAsync(run => run.Id == id, cancellationToken);

    public Task<ProductPublicationRun?> GetActiveForTenantAsync(
        Guid tenantId,
        Marketplace marketplace,
        CancellationToken cancellationToken = default) =>
        db.ProductPublicationRuns
            .Where(run =>
                run.TenantId == tenantId
                && run.Marketplace == marketplace
                && (run.Status == PublicationStatus.Pending || run.Status == PublicationStatus.Running))
            .OrderByDescending(run => run.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductPublicationRun>> ListRecentAsync(
        Guid tenantId,
        Marketplace marketplace,
        int limit,
        CancellationToken cancellationToken = default) =>
        await db.ProductPublicationRuns
            .Where(run => run.TenantId == tenantId && run.Marketplace == marketplace)
            .OrderByDescending(run => run.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<ProductPublicationRun?> TryClaimNextPendingAsync(
        IReadOnlyCollection<Guid>? tenantIds = null,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var tenantFilter = tenantIds is { Count: > 0 } ? tenantIds.ToArray() : null;

        var pendingQuery = tenantFilter is null
            ? db.ProductPublicationRuns
                .FromSqlInterpolated($"""
                    SELECT id, tenant_id, marketplace_code, status, created_at, started_at, completed_at,
                           total_items, processed_items, published_items, failed_items, error_message
                    FROM channels.product_publication_runs
                    WHERE status = {"pending"}
                    ORDER BY created_at
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                    """)
            : db.ProductPublicationRuns
                .FromSqlInterpolated($"""
                    SELECT id, tenant_id, marketplace_code, status, created_at, started_at, completed_at,
                           total_items, processed_items, published_items, failed_items, error_message
                    FROM channels.product_publication_runs
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

        var run = await db.ProductPublicationRuns
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

    public async Task AddAsync(ProductPublicationRun run, CancellationToken cancellationToken = default) =>
        await db.ProductPublicationRuns.AddAsync(run, cancellationToken);

    // run zaten izleniyor; append-only hata child'ları SaveChangesAsync içinde Added'e çevrilir.
    public void Update(ProductPublicationRun run) => _ = run;
}
