using Channels.Application.Imports.ProcessProductImport;
using Channels.Domain.Imports;
using Channels.Infrastructure.Options;
using Microsoft.Extensions.Options;
using SharedKernel.Tenancy;

namespace Pimly.Channels.Worker.Imports;

/// <summary>
/// Ürün import kuyruğunu işleyen arka plan servisi. İki scope'lu pompa deseni kullanır:
/// tenant'sız scope kuyruğu claim eder (FOR UPDATE SKIP LOCKED); ikinci scope'ta ambient tenant,
/// run'ın tenant'ına set edilip işleme yapılır — Catalog yazmaları tenant'ı buradan alır.
/// </summary>
internal sealed class ProductImportBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ChannelsOptions> options,
    ILogger<ProductImportBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.WorkerPollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = false;

            try
            {
                processed = await TryProcessNextAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Product import worker iteration failed.");
            }

            if (!processed)
            {
                try
                {
                    await Task.Delay(pollInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task<bool> TryProcessNextAsync(CancellationToken stoppingToken)
    {
        // Scope A: tenant bağlamı yokken kuyruk claim edilir (query filter'lar devre dışı).
        Guid runId;
        Guid tenantId;

        await using (var claimScope = scopeFactory.CreateAsyncScope())
        {
            var importRuns = claimScope.ServiceProvider.GetRequiredService<IProductImportRunRepository>();
            var run = await importRuns.TryClaimNextPendingAsync(stoppingToken);
            if (run is null)
            {
                return false;
            }

            runId = run.Id;
            tenantId = run.TenantId;
        }

        // Scope B: başka hiçbir servis resolve edilmeden ÖNCE ambient tenant set edilir;
        // böylece DbContext'ler (model cache anahtarı + query filter + stamping) doğru tenant'la kurulur.
        await using (var processScope = scopeFactory.CreateAsyncScope())
        {
            var tenantContext = processScope.ServiceProvider.GetRequiredService<AmbientTenantContext>();
            tenantContext.Set(tenantId);

            var handler = processScope.ServiceProvider.GetRequiredService<IProcessProductImportHandler>();
            var result = await handler.ExecuteAsync(runId, stoppingToken);

            if (result.IsFailure)
            {
                logger.LogWarning(
                    "Product import run {RunId} processing returned failure: {Error}",
                    runId,
                    result.Error.Message);
            }
        }

        return true;
    }
}
