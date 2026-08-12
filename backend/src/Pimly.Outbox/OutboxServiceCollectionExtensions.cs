using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Pimly.Outbox;

/// <summary>Bir modülün outbox dağıtım servislerini DI'a kaydeder.</summary>
public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Tip registry'sini, dispatcher'ı ve processor'ı kaydeder. Integration olay tipleri verilen
    /// assembly'lerden (tipik olarak modülün domain assembly'si) taranır. Handler'lar çağıran tarafça
    /// ayrıca kaydedilir.
    /// </summary>
    /// <typeparam name="TDbContext">Outbox tablosunu barındıran modül DbContext'i.</typeparam>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="eventAssemblies">Integration olay tiplerinin taranacağı assembly'ler.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddOutboxDispatching<TDbContext>(
        this IServiceCollection services,
        params Assembly[] eventAssemblies)
        where TDbContext : DbContext, IOutboxDbContext
    {
        services.AddSingleton(IntegrationEventTypeRegistry<TDbContext>.FromAssemblies(eventAssemblies));
        services.TryAddDispatcher();
        services.AddScoped<OutboxProcessor<TDbContext>>();

        return services;
    }

    private static void TryAddDispatcher(this IServiceCollection services)
    {
        // Dispatcher context'ten bağımsızdır; birden çok modül kaydedildiğinde tek örnek yeterlidir.
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IntegrationEventDispatcher)))
        {
            return;
        }

        services.AddScoped<IntegrationEventDispatcher>();
    }
}
