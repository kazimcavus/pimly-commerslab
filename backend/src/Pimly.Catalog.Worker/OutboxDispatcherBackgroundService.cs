using Catalog.Infrastructure.Outbox;

namespace Pimly.Catalog.Worker;

/// <summary>Catalog outbox'ını periyodik olarak işleyip integration olaylarını dağıtır.</summary>
internal sealed class OutboxDispatcherBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcherBackgroundService> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Catalog outbox dispatcher başladı. Aralık={IntervalSeconds}s, grup={BatchSize}.",
                PollInterval.TotalSeconds,
                BatchSize);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
                await processor.ProcessPendingAsync(BatchSize, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Outbox dağıtım döngüsü beklenmeyen bir hata verdi.");
                }
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
