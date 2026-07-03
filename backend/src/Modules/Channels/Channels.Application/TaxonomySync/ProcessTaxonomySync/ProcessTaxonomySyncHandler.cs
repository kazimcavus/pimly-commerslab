using Channels.Application.ExternalCatalog;
using Channels.Domain;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.Marketplaces;
using Channels.Domain.TaxonomySync;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Channels.Application.TaxonomySync.ProcessTaxonomySync;

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
    IMarketplaceTaxonomyClientResolver taxonomyClientResolver,
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

        var marketplace = syncRun.Marketplace;

        var clientResult = taxonomyClientResolver.Resolve(marketplace);
        if (clientResult.IsFailure)
        {
            await FailRunAsync(syncRun, clientResult.Error.Message, cancellationToken);
            return Result.Success(true);
        }

        try
        {
            var fetchResult = await clientResult.Value.FetchAllCategoriesAsync(marketplace, cancellationToken);
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
                    marketplace,
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
                    "Taxonomy sync {SyncRunId} completed for marketplace {Marketplace} with {CategoryCount} categories.",
                    syncRun.Id,
                    marketplace.Code,
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
                    "Taxonomy sync {SyncRunId} failed for marketplace {Marketplace}.",
                    syncRun.Id,
                    marketplace.Code);
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
