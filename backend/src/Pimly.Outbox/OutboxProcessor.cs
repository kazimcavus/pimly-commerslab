using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Pimly.Outbox;

/// <summary>
/// Bir modülün outbox'ındaki işlenmemiş integration olaylarını en eskiden başlayarak okuyup
/// handler'lara dağıtır. Her mesaj kendi tenant bağlamında işlenir; başarıda işaretlenir,
/// hatada denemesi artırılır ve hata kaydedilir.
/// </summary>
/// <typeparam name="TDbContext">Outbox tablosunu barındıran modül DbContext'i.</typeparam>
public sealed class OutboxProcessor<TDbContext>(
    TDbContext dbContext,
    IServiceScopeFactory scopeFactory,
    IntegrationEventTypeRegistry<TDbContext> typeRegistry,
    ILogger<OutboxProcessor<TDbContext>> logger)
    where TDbContext : DbContext, IOutboxDbContext
{
    /// <summary>Bekleyen olay grubunu işler; işlenmeye çalışılan mesaj sayısını döner.</summary>
    /// <param name="batchSize">Tek turda işlenecek azami mesaj sayısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İşlenmeye çalışılan mesaj sayısı.</returns>
    public async Task<int> ProcessPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await DispatchAsync(message, cancellationToken);
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.Error = ex.Message;

                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(
                        ex,
                        "Outbox mesajı dağıtılamadı: {MessageId} ({Type}), deneme {Attempts}.",
                        message.Id,
                        message.Type,
                        message.Attempts);
                }
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }

    private async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var eventType = typeRegistry.Resolve(message.Type)
            ?? throw new InvalidOperationException($"Bilinmeyen integration olay tipi: '{message.Type}'.");

        var integrationEvent = (IntegrationEvent)JsonSerializer.Deserialize(
            message.Payload, eventType, OutboxSerialization.JsonOptions)!;

        // Her mesaj kendi tenant bağlamında işlenir; handler'lar bu scope'ta çözülür.
        await using var scope = scopeFactory.CreateAsyncScope();

        if (scope.ServiceProvider.GetService<ITenantContext>() is AmbientTenantContext tenantContext)
        {
            tenantContext.Set(message.TenantId);
        }

        var dispatcher = scope.ServiceProvider.GetRequiredService<IntegrationEventDispatcher>();
        await dispatcher.DispatchAsync(integrationEvent, cancellationToken);
    }
}
