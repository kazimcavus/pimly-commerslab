using Channels.Application.Listings.ContentSync;
using Channels.Domain;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.Connections;
using Channels.Domain.Listings;
using Channels.Domain.Publications;
using Microsoft.Extensions.Logging;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Channels.Application.Publications.ProcessPublication;

/// <summary>
/// Claim edilmiş yayın run'ını işler: pazaryeri kategorisine eşlenmiş kategorilerdeki henüz
/// listelenmemiş kalemler için listeleme kaydı açar, ardından içerik senkronunu çalıştırarak
/// kartları pazaryerine gönderir.
/// </summary>
/// <remarks>
/// <para><b>Neden iki adım:</b> Yeni ürün gönderimi ile içerik güncellemesi aynı payload ve aynı
/// pazaryeri ucunu kullanır. Run yalnızca <em>kaydı açar</em>; teslimatı tek bir yol
/// (<see cref="ISyncListingContentHandler"/>) üstlenir, böylece delta ve backoff mantığı tek yerde kalır.</para>
/// <para><b>Ön koşullar:</b> Run claim edilip Running yapılmış, ambient tenant run'ın tenant'ına set
/// edilmiş olmalı.</para>
/// <para><b>Kapsam:</b> Kategorisi eşlenmemiş kalemler hiç kaydedilmez — pazaryerinde nereye
/// listeleneceği bilinmediği için gönderilemezler.</para>
/// </remarks>
public sealed class ProcessPublicationHandler(
    IProductPublicationRunRepository publicationRuns,
    IMarketplaceConnectionRepository connections,
    ICategoryChannelMappingRepository categoryMappings,
    ICatalogListingSourceGateway catalogSources,
    IProductListingRepository listings,
    ISyncListingContentHandler contentSync,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ProcessPublicationHandler> logger) : IProcessPublicationHandler
{
    private const int MappingPageSize = 100;

    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await publicationRuns.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return Result.Failure(Error.NotFound("Publication run not found."));
        }

        if (run.Status != PublicationStatus.Running)
        {
            return Result.Failure(Error.Conflict("Publication run is not in running state."));
        }

        if (tenantContext.TenantId != run.TenantId)
        {
            return Result.Failure(Error.Conflict("Tenant context does not match the publication run."));
        }

        var marketplace = run.Marketplace;

        var connection = await connections.GetByMarketplaceAsync(marketplace, cancellationToken);
        if (connection is null || !connection.IsEnabled)
        {
            await FailRunAsync(run, "Marketplace connection is missing or disabled.", cancellationToken);
            return Result.Success();
        }

        try
        {
            var enrolled = await EnrollListingsAsync(marketplace, run, cancellationToken);

            run.UpdateProgress(0, 0, 0, enrolled.Total);
            await SaveRunAsync(run, cancellationToken);

            // Teslimat tek yoldan: içerik senkronu kirli (yeni açılan dahil) listelemeleri gönderir.
            var syncResult = await contentSync.ExecuteAsync(marketplace.Code, cancellationToken);
            if (syncResult.IsFailure)
            {
                await FailRunAsync(run, syncResult.Error.Message, cancellationToken);
                return Result.Success();
            }

            var summary = syncResult.Value;
            var published = summary.Created + summary.Updated;

            run.UpdateProgress(summary.Examined, published, summary.Failed, enrolled.Total);
            var completeResult = run.MarkCompleted(timeProvider.GetUtcNow());
            if (completeResult.IsFailure)
            {
                return completeResult;
            }

            await SaveRunAsync(run, cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Publication {RunId} finished for tenant {TenantId}: {Enrolled} yeni kayıt, {Created} oluşturuldu, {Updated} güncellendi, {Failed} hata.",
                    run.Id,
                    run.TenantId,
                    enrolled.Created,
                    summary.Created,
                    summary.Updated,
                    summary.Failed);
            }

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Publication {RunId} failed for tenant {TenantId}.", run.Id, run.TenantId);
            await FailRunAsync(run, ex.Message, cancellationToken);
            return Result.Success();
        }
    }

    /// <summary>
    /// Eşlenmiş kategorilerdeki kalemler için eksik listeleme kayıtlarını açar. Var olan kayıtlara
    /// dokunulmaz; yeni kayıtlar baştan kirli olduğu için senkron turunda gönderilirler.
    /// </summary>
    private async Task<EnrollmentResult> EnrollListingsAsync(
        Marketplace marketplace,
        ProductPublicationRun run,
        CancellationToken cancellationToken)
    {
        var categoryIds = await ListMappedCategoryIdsAsync(marketplace, cancellationToken);
        if (categoryIds.Count == 0)
        {
            run.AddError(Guid.Empty, "Bu pazaryeri için eşlenmiş kategori yok; yayınlanacak kalem bulunamadı.");
            return new EnrollmentResult(0, 0);
        }

        var itemIds = await catalogSources.ListItemIdsByCategoriesAsync(categoryIds, cancellationToken);
        if (itemIds.Count == 0)
        {
            return new EnrollmentResult(0, 0);
        }

        var existing = await listings.ListByProductItemsAsync(marketplace, itemIds, cancellationToken);
        var alreadyListed = existing.Select(listing => listing.ProductItemId).ToHashSet();

        var now = timeProvider.GetUtcNow();
        var created = new List<ProductListing>();

        foreach (var itemId in itemIds.Where(itemId => !alreadyListed.Contains(itemId)))
        {
            var createResult = ProductListing.Create(run.TenantId, marketplace, itemId, now);
            if (createResult.IsFailure)
            {
                run.AddError(itemId, createResult.Error.Message);
                continue;
            }

            created.Add(createResult.Value);
        }

        if (created.Count > 0)
        {
            await listings.AddRangeAsync(created, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new EnrollmentResult(itemIds.Count, created.Count);
    }

    private async Task<IReadOnlyList<Guid>> ListMappedCategoryIdsAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken)
    {
        var categoryIds = new List<Guid>();

        for (var page = 1; ; page++)
        {
            var paginationResult = Pagination.Create(page, MappingPageSize);
            if (paginationResult.IsFailure)
            {
                break;
            }

            var mappings = await categoryMappings.ListAsync(
                marketplace,
                catalogCategoryId: null,
                paginationResult.Value,
                cancellationToken);

            if (mappings.Count == 0)
            {
                break;
            }

            categoryIds.AddRange(mappings.Select(mapping => mapping.CatalogCategoryId));

            if (mappings.Count < MappingPageSize)
            {
                break;
            }
        }

        return categoryIds;
    }

    private async Task SaveRunAsync(ProductPublicationRun run, CancellationToken cancellationToken)
    {
        publicationRuns.Update(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task FailRunAsync(ProductPublicationRun run, string message, CancellationToken cancellationToken)
    {
        var failResult = run.MarkFailed(timeProvider.GetUtcNow(), message);
        if (failResult.IsFailure)
        {
            return;
        }

        await SaveRunAsync(run, cancellationToken);
    }

    private sealed record EnrollmentResult(int Total, int Created);
}
