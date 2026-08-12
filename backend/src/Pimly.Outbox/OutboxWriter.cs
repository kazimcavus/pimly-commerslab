using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Pimly.Outbox;

/// <summary>Change tracker'daki integration olaylarını outbox'a yazan ortak yardımcı.</summary>
public static class OutboxWriter
{
    /// <summary>
    /// İzlenen aggregate'lerin <see cref="IntegrationEvent"/> tipindeki alan olaylarını outbox'a
    /// yazar ve olayları temizler. <c>SaveChangesAsync</c> içinden, base çağrısından <em>önce</em>
    /// çağrılmalıdır — böylece olaylar aggregate değişiklikleriyle aynı transaction'a girer.
    /// </summary>
    /// <param name="dbContext">Olayların toplanacağı DbContext.</param>
    /// <param name="tenantId">Olayları üreten tenant; olay varken boş olamaz.</param>
    /// <exception cref="InvalidOperationException">Tenant bağlamı olmadan integration olay üretildiyse.</exception>
    public static void WriteOutboxMessages(this DbContext dbContext, Guid tenantId)
    {
        var holders = dbContext.ChangeTracker.Entries()
            .Select(entry => entry.Entity)
            .OfType<IHasDomainEvents>()
            .Where(holder => holder.DomainEvents.Count > 0)
            .ToList();

        if (holders.Count == 0)
        {
            return;
        }

        var messages = new List<OutboxMessage>();
        foreach (var holder in holders)
        {
            foreach (var integrationEvent in holder.DomainEvents.OfType<IntegrationEvent>())
            {
                var eventType = integrationEvent.GetType();
                messages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Type = eventType.FullName!,
                    Payload = JsonSerializer.Serialize(
                        integrationEvent,
                        eventType,
                        OutboxSerialization.JsonOptions),
                    OccurredOnUtc = integrationEvent.OccurredOnUtc,
                });
            }

            holder.ClearDomainEvents();
        }

        if (messages.Count == 0)
        {
            return;
        }

        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant id is required to persist integration events.");
        }

        dbContext.Set<OutboxMessage>().AddRange(messages);
    }
}
