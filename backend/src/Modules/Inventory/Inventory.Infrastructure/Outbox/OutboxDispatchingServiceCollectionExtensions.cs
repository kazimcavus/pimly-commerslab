using Inventory.Domain.StockLevels.Events;
using Inventory.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Pimly.Outbox;

namespace Inventory.Infrastructure.Outbox;

/// <summary>Inventory outbox dağıtım servislerini DI'a kaydeder.</summary>
public static class OutboxDispatchingServiceCollectionExtensions
{
    /// <summary>
    /// Ortak outbox mekanizmasını Inventory DbContext'i için kaydeder. Integration olay tipleri
    /// Inventory.Domain assembly'sinden taranır; handler'lar çağıran tarafça ayrıca kaydedilir.
    /// </summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddInventoryOutboxDispatching(this IServiceCollection services) =>
        services.AddOutboxDispatching<InventoryDbContext>(typeof(StockLevelChanged).Assembly);
}
