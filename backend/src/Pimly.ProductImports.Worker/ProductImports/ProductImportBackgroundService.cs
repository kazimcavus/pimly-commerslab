using Channels.Application.ProductImports.ProcessProductImport;
using Channels.Domain.ProductImports;
using Microsoft.Extensions.Options;
using Pimly.ProductImports.Worker.Options;
using SharedKernel.Tenancy;

namespace Pimly.ProductImports.Worker.ProductImports;

/// <summary>
/// Ürün import kuyruğunu işleyen arka plan servisi. İki scope'lu pompa deseni kullanır:
/// tenant'sız scope kuyruğu claim eder (FOR UPDATE SKIP LOCKED); ikinci scope'ta ambient tenant,
/// run'ın tenant'ına set edilip işleme yapılır — Catalog yazmaları tenant'ı buradan alır.
/// Yalnızca konfigüre edilen tenant'ların (<see cref="ProductImportsWorkerOptions.TenantIds"/>)
/// run'ları claim edilir; böylece worker instance'ları tenant bazında izole çalıştırılabilir.
/// </summary>
internal sealed class ProductImportBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ProductImportsWorkerOptions> options,
    ILogger<ProductImportBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollIntervalSeconds));
        var tenantFilter = options.Value.TenantIds.ToArray();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Product import worker started for tenants: {TenantIds}.",
                string.Join(", ", tenantFilter));
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = false;

            try
            {
                processed = await TryProcessNextAsync(tenantFilter, stoppingToken);
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

    private async Task<bool> TryProcessNextAsync(IReadOnlyCollection<Guid> tenantFilter, CancellationToken stoppingToken)
    {
        // Scope A: tenant bağlamı yokken kuyruk claim edilir (query filter'lar devre dışı).
        Guid runId;
        Guid tenantId;

        await using (var claimScope = scopeFactory.CreateAsyncScope())
        {
            var importRuns = claimScope.ServiceProvider.GetRequiredService<IProductImportRunRepository>();
            var run = await importRuns.TryClaimNextPendingAsync(tenantFilter, stoppingToken);
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
