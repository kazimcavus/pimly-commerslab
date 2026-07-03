using Channels.Application.Taxonomy.ProcessTaxonomySync;
using Channels.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Pimly.Channels.Worker.Taxonomy;

/// <summary>Pending taxonomy sync job'larını poll edip işleyen arka plan servisi.</summary>
internal sealed class TaxonomySyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ChannelsOptions> options,
    ILogger<TaxonomySyncBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.WorkerPollIntervalSeconds));

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Taxonomy sync worker started with poll interval {PollIntervalSeconds}s.",
                pollInterval.TotalSeconds);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var processedJob = false;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<IProcessTaxonomySyncHandler>();
                var result = await handler.ExecuteAsync(stoppingToken);

                if (result.IsFailure)
                {
                    if (logger.IsEnabled(LogLevel.Error))
                    {
                        logger.LogError(
                            "Taxonomy sync worker iteration failed: {ErrorCode} {ErrorMessage}",
                            result.Error.Code,
                            result.Error.Message);
                    }
                }
                else
                {
                    processedJob = result.Value;
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
                    logger.LogError(ex, "Taxonomy sync worker iteration threw an unexpected exception.");
                }
            }

            if (!processedJob)
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
        }
    }
}
