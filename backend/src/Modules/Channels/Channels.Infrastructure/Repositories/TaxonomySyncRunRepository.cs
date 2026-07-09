using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Channels.Infrastructure.Repositories;

/// <summary>TaxonomySyncRun aggregate'leri için EF Core tabanlı depo.</summary>
internal sealed class TaxonomySyncRunRepository(ChannelsDbContext db, TimeProvider timeProvider)
    : ITaxonomySyncRunRepository
{
    public Task<TaxonomySyncRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.TaxonomySyncRuns.FirstOrDefaultAsync(syncRun => syncRun.Id == id, cancellationToken);

    public Task<TaxonomySyncRun?> GetActiveForMarketplaceAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default) =>
        db.TaxonomySyncRuns
            .Where(syncRun =>
                syncRun.Marketplace == marketplace
                && (syncRun.Status == TaxonomySyncStatus.Pending
                    || syncRun.Status == TaxonomySyncStatus.Running))
            .OrderByDescending(syncRun => syncRun.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<TaxonomySyncRun?> GetLatestCompletedForMarketplaceAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default) =>
        db.TaxonomySyncRuns
            .Where(syncRun =>
                syncRun.Marketplace == marketplace
                && syncRun.Status == TaxonomySyncStatus.Completed)
            .OrderByDescending(syncRun => syncRun.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> HasSyncRunSinceAsync(
        Marketplace marketplace,
        DateTimeOffset since,
        CancellationToken cancellationToken = default) =>
        db.TaxonomySyncRuns.AnyAsync(
            syncRun => syncRun.Marketplace == marketplace && syncRun.CreatedAt >= since,
            cancellationToken);

    public async Task<TaxonomySyncRun?> TryClaimNextPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var pendingId = await db.TaxonomySyncRuns
            .FromSqlInterpolated($"""
                SELECT id, marketplace_code, status, created_at, started_at, completed_at,
                       processed_count, total_estimate, error_message
                FROM channels.taxonomy_sync_runs
                WHERE status = {"pending"}
                ORDER BY created_at
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .Select(syncRun => syncRun.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (pendingId == Guid.Empty)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var syncRun = await db.TaxonomySyncRuns
            .FirstAsync(syncRun => syncRun.Id == pendingId, cancellationToken);

        var markResult = syncRun.MarkRunning(timeProvider.GetUtcNow());
        if (markResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return syncRun;
    }

    public async Task AddAsync(TaxonomySyncRun syncRun, CancellationToken cancellationToken = default) =>
        await db.TaxonomySyncRuns.AddAsync(syncRun, cancellationToken);

    public void Update(TaxonomySyncRun syncRun) =>
        db.TaxonomySyncRuns.Update(syncRun);
}
