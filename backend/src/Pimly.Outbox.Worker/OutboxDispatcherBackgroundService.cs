using Microsoft.EntityFrameworkCore;
using Pimly.Outbox;

namespace Pimly.Outbox.Worker;

/// <summary>
/// Bir modülün outbox'ını periyodik olarak işleyip integration olaylarını dağıtır. Her modül kendi
/// tablosuna sahip olduğu için bu servis DbContext başına ayrı örneklenir.
/// </summary>
/// <typeparam name="TDbContext">Outbox tablosunu barındıran modül DbContext'i.</typeparam>
internal sealed class OutboxDispatcherBackgroundService<TDbContext>(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcherBackgroundService<TDbContext>> logger) : BackgroundService
    where TDbContext : DbContext, IOutboxDbContext
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "{Module} outbox dispatcher başladı. Aralık={IntervalSeconds}s, grup={BatchSize}.",
                typeof(TDbContext).Name,
                PollInterval.TotalSeconds,
                BatchSize);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor<TDbContext>>();
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
                    logger.LogError(
                        ex,
                        "{Module} outbox dağıtım döngüsü beklenmeyen bir hata verdi.",
                        typeof(TDbContext).Name);
                }
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
