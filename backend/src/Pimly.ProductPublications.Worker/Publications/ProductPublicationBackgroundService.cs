using Channels.Application.Publications.ProcessPublication;
using Channels.Domain.Publications;
using Microsoft.Extensions.Options;
using Pimly.ProductPublications.Worker.Options;
using SharedKernel.Tenancy;

namespace Pimly.ProductPublications.Worker.Publications;

/// <summary>
/// Ürün yayın (publish) kuyruğunu işleyen arka plan servisi. ProductImportBackgroundService ile aynı
/// iki-scope'lu pompa desenini kullanır: tenant'sız scope kuyruğu claim eder; ikinci scope'ta ambient
/// tenant run'ın tenant'ına set edilip işleme yapılır.
/// </summary>
internal sealed class ProductPublicationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ProductPublicationsWorkerOptions> options,
    ILogger<ProductPublicationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollIntervalSeconds));
        var tenantFilter = options.Value.TenantIds.ToArray();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Product publication worker started for tenants: {TenantIds}.",
                tenantFilter.Length > 0 ? string.Join(", ", tenantFilter) : "(all)");
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
                logger.LogError(ex, "Product publication worker iteration failed.");
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
        Guid runId;
        Guid tenantId;

        await using (var claimScope = scopeFactory.CreateAsyncScope())
        {
            var publicationRuns = claimScope.ServiceProvider.GetRequiredService<IProductPublicationRunRepository>();
            var run = await publicationRuns.TryClaimNextPendingAsync(tenantFilter, stoppingToken);
            if (run is null)
            {
                return false;
            }

            runId = run.Id;
            tenantId = run.TenantId;
        }

        await using (var processScope = scopeFactory.CreateAsyncScope())
        {
            var tenantContext = processScope.ServiceProvider.GetRequiredService<AmbientTenantContext>();
            tenantContext.Set(tenantId);

            var handler = processScope.ServiceProvider.GetRequiredService<IProcessPublicationHandler>();
            var result = await handler.ExecuteAsync(runId, stoppingToken);

            if (result.IsFailure)
            {
                logger.LogWarning(
                    "Publication run {RunId} processing returned failure: {Error}",
                    runId,
                    result.Error.Message);
            }
        }

        return true;
    }
}
