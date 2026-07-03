using Channels.Application.Contracts;
using Channels.Application.Options;
using Channels.Application.Taxonomy.EnqueueTaxonomySync;
using Channels.Application.Taxonomy.Scheduling;
using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Channels.Application.Taxonomy.RunScheduledTaxonomySync;

/// <summary>
/// Yapılandırılmış UTC zaman dilimine göre aktif pazaryerleri için eksik taxonomy sync job'larını
/// otomatik olarak kuyruğa alır.
/// </summary>
/// <remarks>
/// <para><b>Amaç:</b> Periyodik taxonomy güncellemesini zamanlayıcı/background servis üzerinden
/// otomatikleştirir; her aktif pazaryeri için güncel dilimde henüz sync yoksa kuyruğa ekler.</para>
/// <para><b>Ön koşullar:</b> <see cref="Options.TaxonomySyncScheduleOptions"/> etkin olmalı ve geçerli
/// UTC saat listesi içermelidir.</para>
/// <para><b>Ana akış:</b> Zamanlama devre dışıysa 0 döner → geçerli dilim başlangıcı hesaplanır →
/// her aktif pazaryeri için dilim içinde sync var mı kontrol edilir → yoksa
/// <see cref="EnqueueTaxonomySync.IEnqueueTaxonomySyncHandler"/> çağrılır → kuyruğa alınan sayı döner.</para>
/// <para><b>Hata durumları:</b> Geçersiz zamanlama yapılandırması (Validation). Conflict veya diğer
/// enqueue hataları loglanır ve ilgili pazaryeri atlanır.</para>
/// <para><b>API:</b> Yalnızca dahili zamanlayıcı/background servis tarafından tetiklenir; HTTP API'de
/// doğrudan endpoint yoktur.</para>
/// </remarks>
public sealed class RunScheduledTaxonomySyncHandler(
    IOptions<TaxonomySyncScheduleOptions> options,
    ITaxonomySyncRunRepository syncRuns,
    IEnqueueTaxonomySyncHandler enqueueTaxonomySync,
    TimeProvider timeProvider,
    ILogger<RunScheduledTaxonomySyncHandler> logger) : IRunScheduledTaxonomySyncHandler
{
    /// <inheritdoc/>
    public async Task<Result<int>> ExecuteAsync(
        RunScheduledTaxonomySyncCommand command,
        CancellationToken cancellationToken = default)
    {
        _ = command;

        var schedule = options.Value;
        if (!schedule.Enabled)
        {
            return Result.Success(0);
        }

        IReadOnlyList<TimeOnly> scheduleTimes;
        try
        {
            scheduleTimes = TaxonomySyncScheduleCalculator.ParseTimesUtc(schedule.TimesUtc);
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            return Result.Failure<int>(Error.Validation("Invalid taxonomy sync schedule configuration."));
        }

        var now = timeProvider.GetUtcNow();
        var slotStart = TaxonomySyncScheduleCalculator.GetCurrentSlotStartUtc(now, scheduleTimes);

        var enqueuedCount = 0;
        foreach (var marketplace in MarketplaceRegistry.ListActive())
        {
            if (await syncRuns.HasSyncRunSinceAsync(marketplace.Key, slotStart, cancellationToken))
            {
                continue;
            }

            var enqueueResult = await enqueueTaxonomySync.ExecuteAsync(
                new EnqueueTaxonomySyncCommand(marketplace.Key),
                cancellationToken);

            if (enqueueResult.IsFailure)
            {
                if (enqueueResult.Error.Code == ErrorCodes.Conflict)
                {
                    continue;
                }

                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(
                        "Scheduled taxonomy sync skipped for marketplace {MarketplaceKey}: {ErrorMessage}",
                        marketplace.Key.Value,
                        enqueueResult.Error.Message);
                }

                continue;
            }

            enqueuedCount++;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Scheduled taxonomy sync enqueued for marketplace {MarketplaceKey} in slot starting {SlotStartUtc}. SyncRunId={SyncRunId}",
                    marketplace.Key.Value,
                    slotStart,
                    enqueueResult.Value.Id);
            }
        }

        return Result.Success(enqueuedCount);
    }
}
