using Catalog.Domain.Products.Events;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Catalog.Infrastructure.Outbox;

/// <summary>Catalog outbox dağıtım servislerini DI'a kaydeder.</summary>
public static class OutboxDispatchingServiceCollectionExtensions
{
    /// <summary>
    /// Tip registry'sini, dispatcher'ı ve outbox processor'ı kaydeder. Integration olay
    /// tipleri Catalog.Domain assembly'sinden taranarak toplanır. Handler'lar çağıran
    /// tarafça ayrıca kaydedilir.
    /// </summary>
    public static IServiceCollection AddCatalogOutboxDispatching(this IServiceCollection services)
    {
        var eventTypes = typeof(ProductItemCreated).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false } && typeof(IntegrationEvent).IsAssignableFrom(type))
            .ToArray();

        services.AddSingleton(new IntegrationEventTypeRegistry(eventTypes));
        services.AddScoped<IntegrationEventDispatcher>();
        services.AddScoped<OutboxProcessor>();

        return services;
    }
}
