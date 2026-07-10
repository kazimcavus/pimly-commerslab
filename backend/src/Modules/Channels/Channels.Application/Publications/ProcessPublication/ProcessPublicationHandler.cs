using Channels.Application.Connections;
using Channels.Domain;
using Channels.Domain.Connections;
using Channels.Domain.Publications;
using Microsoft.Extensions.Logging;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Channels.Application.Publications.ProcessPublication;

/// <summary>
/// Claim edilmiş yayın run'ını işler: Pricing'de kararlaştırılmış kanal fiyatlarını okur ve her kalemi
/// pazaryerinde listeler (publish). ProcessProductImport'un outbound aynasıdır.
/// </summary>
/// <remarks>
/// Ön koşullar: run kuyruktan claim edilip Running yapılmış, ambient tenant run'ın tenant'ına set edilmiş
/// olmalı. Altyapı hatası run'ı Failed yapar; kalem düzeyi hata kaydedilip diğer kalemlerle devam edilir.
/// </remarks>
public sealed class ProcessPublicationHandler(
    IProductPublicationRunRepository publicationRuns,
    IMarketplaceConnectionRepository connections,
    IPricingChannelPriceGateway channelPrices,
    IMarketplaceListingClientResolver clientResolver,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ProcessPublicationHandler> logger) : IProcessPublicationHandler
{
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

        var clientResult = clientResolver.Resolve(marketplace);
        if (clientResult.IsFailure)
        {
            await FailRunAsync(run, "Marketplace listing client is not configured.", cancellationToken);
            return Result.Success();
        }

        var connection = await connections.GetByMarketplaceAsync(marketplace, cancellationToken);
        if (connection is null || !connection.IsEnabled)
        {
            await FailRunAsync(run, "Marketplace connection is missing or disabled.", cancellationToken);
            return Result.Success();
        }

        var credentials = new MarketplaceCredentials(connection.SellerId, connection.ApiKey, connection.ApiSecret);

        try
        {
            var decidedPrices = await channelPrices.ListForMarketplaceAsync(marketplace, cancellationToken);

            var total = decidedPrices.Count;
            var processed = 0;
            var published = 0;
            var failed = 0;

            run.UpdateProgress(0, 0, 0, total);
            await SaveRunAsync(run, cancellationToken);

            foreach (var price in decidedPrices)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var publishResult = await clientResult.Value.PublishAsync(
                    marketplace,
                    credentials,
                    new MarketplaceListingRequest(price.ProductItemId, price.Amount, price.CompareAtAmount, price.Currency),
                    cancellationToken);

                if (publishResult.IsSuccess)
                {
                    published++;
                }
                else
                {
                    failed++;
                    run.AddError(price.ProductItemId, publishResult.Error.Message);
                }

                processed++;
            }

            run.UpdateProgress(processed, published, failed, total);
            var completeResult = run.MarkCompleted(timeProvider.GetUtcNow());
            if (completeResult.IsFailure)
            {
                return completeResult;
            }

            await SaveRunAsync(run, cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Publication {RunId} finished for tenant {TenantId}: {Published} published, {Failed} failed.",
                    run.Id,
                    run.TenantId,
                    published,
                    failed);
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
}
