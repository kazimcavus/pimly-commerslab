using Channels.Application.Options;
using Channels.Application.Taxonomy.RunScheduledTaxonomySync;
using Microsoft.Extensions.Options;

namespace Pimly.Channels.Worker.Taxonomy;

/// <summary>Gün içindeki taxonomy sync zaman dilimlerini kontrol edip eksik job'ları kuyruğa alır.</summary>
internal sealed class ScheduledTaxonomySyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<TaxonomySyncScheduleOptions> options,
    ILogger<ScheduledTaxonomySyncBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var schedule = options.Value;
        if (!schedule.Enabled)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Scheduled taxonomy sync is disabled.");
            }

            return;
        }

        var checkInterval = TimeSpan.FromSeconds(Math.Max(15, schedule.CheckIntervalSeconds));

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Scheduled taxonomy sync checker started. TimesUtc=[{TimesUtc}], interval={CheckIntervalSeconds}s.",
                string.Join(", ", schedule.TimesUtc),
                checkInterval.TotalSeconds);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<IRunScheduledTaxonomySyncHandler>();
                var result = await handler.ExecuteAsync(new RunScheduledTaxonomySyncCommand(), stoppingToken);

                if (result.IsFailure && logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(
                        "Scheduled taxonomy sync tick failed: {ErrorCode} {ErrorMessage}",
                        result.Error.Code,
                        result.Error.Message);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Scheduled taxonomy sync tick threw an unexpected exception.");
                }
            }

            await Task.Delay(checkInterval, stoppingToken);
        }
    }
}
