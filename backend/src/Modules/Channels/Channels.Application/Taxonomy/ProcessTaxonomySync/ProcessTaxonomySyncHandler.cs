using Channels.Domain;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Channels.Application.Taxonomy.ProcessTaxonomySync;

/// <summary>
/// Kuyruktaki bekleyen taxonomy sync job'ını claim eder, pazaryeri kategorilerini indirir ve yerel
/// cache'e toplu olarak yazar.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Arka plan worker'ının pazaryeri kategori ağacını periyodik olarak güncel tutmasını
/// sağlar; indirilen kategoriler <see cref="IExternalCategoryRepository"/> üzerinde saklanır.</para>
/// <para><b>Ön koşullar:</b> Kuyrukta Pending durumunda en az bir <see cref="TaxonomySyncRun"/> olmalıdır
/// (genellikle <see cref="EnqueueTaxonomySync.EnqueueTaxonomySyncHandler"/> ile oluşturulur).</para>
/// <para><b>Ana akış:</b> Sıradaki pending run claim edilir → pazaryeri doğrulanır →
/// <see cref="IMarketplaceTaxonomyClient"/> ile tüm kategoriler çekilir → 250'lik batch'ler halinde
/// upsert edilir → ilerleme güncellenir → run Completed veya Failed olarak kapatılır.</para>
/// <para><b>Hata durumları:</b> Pazaryeri bulunamadı/pasif (run Failed), API hatası (run Failed),
/// beklenmeyen exception (run Failed). Job yoksa <c>false</c> döner; işlem hatası olsa bile worker
/// döngüsü devam eder.</para>
/// <para><b>API:</b> Yalnızca dahili worker/background servis tarafından kullanılır; HTTP API'de
/// doğrudan endpoint yoktur.</para>
/// </remarks>
public sealed class ProcessTaxonomySyncHandler(
    ITaxonomySyncRunRepository syncRuns,
    IExternalCategoryRepository externalCategories,
    IMarketplaceTaxonomyClient taxonomyClient,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ProcessTaxonomySyncHandler> logger) : IProcessTaxonomySyncHandler
{
    private const int UpsertBatchSize = 250;

    /// <inheritdoc/>
    public async Task<Result<bool>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var syncRun = await syncRuns.TryClaimNextPendingAsync(cancellationToken);
        if (syncRun is null)
        {
            return Result.Success(false);
        }

        var marketplaceResult = MarketplaceRegistry.GetByKey(syncRun.MarketplaceKey);
        if (marketplaceResult.IsFailure)
        {
            await FailRunAsync(syncRun, "Marketplace not found.", cancellationToken);
            return Result.Success(true);
        }

        var marketplace = marketplaceResult.Value;
        if (!marketplace.IsActive)
        {
            await FailRunAsync(syncRun, "Marketplace is not active.", cancellationToken);
            return Result.Success(true);
        }

        try
        {
            var fetchResult = await taxonomyClient.FetchAllCategoriesAsync(marketplace, cancellationToken);
            if (fetchResult.IsFailure)
            {
                await FailRunAsync(syncRun, fetchResult.Error.Message, cancellationToken);
                return Result.Success(true);
            }

            var categories = fetchResult.Value;
            var syncedAt = timeProvider.GetUtcNow();
            var processed = 0;

            syncRun.UpdateProgress(0, categories.Count);
            syncRuns.Update(syncRun);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var batch in categories.Chunk(UpsertBatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var upserts = batch
                    .Select(node => new ExternalCategoryUpsert(
                        node.ExternalId,
                        node.Name,
                        node.ParentExternalId,
                        node.Path,
                        node.IsLeaf))
                    .ToList();

                await externalCategories.UpsertBatchAsync(
                    marketplace.Key,
                    upserts,
                    syncedAt,
                    cancellationToken);

                processed += batch.Length;
                syncRun.UpdateProgress(processed, categories.Count);
                syncRuns.Update(syncRun);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var completeResult = syncRun.MarkCompleted(timeProvider.GetUtcNow(), processed);
            if (completeResult.IsFailure)
            {
                return Result.Failure<bool>(completeResult.Error);
            }

            syncRuns.Update(syncRun);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Taxonomy sync {SyncRunId} completed for marketplace {MarketplaceKey} with {CategoryCount} categories.",
                    syncRun.Id,
                    marketplace.Key.Value,
                    processed);
            }

            return Result.Success(true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(
                    ex,
                    "Taxonomy sync {SyncRunId} failed for marketplace {MarketplaceKey}.",
                    syncRun.Id,
                    marketplace.Key.Value);
            }

            await FailRunAsync(syncRun, ex.Message, cancellationToken);
            return Result.Success(true);
        }
    }

    private async Task FailRunAsync(
        TaxonomySyncRun syncRun,
        string message,
        CancellationToken cancellationToken)
    {
        var failResult = syncRun.MarkFailed(timeProvider.GetUtcNow(), message);
        if (failResult.IsFailure)
        {
            return;
        }

        syncRuns.Update(syncRun);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
