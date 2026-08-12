using Microsoft.Extensions.DependencyInjection;
using Pimly.Outbox;
using Pricing.Domain.ChannelPrices.Events;
using Pricing.Infrastructure.Persistence;

namespace Pricing.Infrastructure.Outbox;

/// <summary>Pricing outbox dağıtım servislerini DI'a kaydeder.</summary>
public static class OutboxDispatchingServiceCollectionExtensions
{
    /// <summary>
    /// Ortak outbox mekanizmasını Pricing DbContext'i için kaydeder. Integration olay tipleri
    /// Pricing.Domain assembly'sinden taranır; handler'lar çağıran tarafça ayrıca kaydedilir.
    /// </summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddPricingOutboxDispatching(this IServiceCollection services) =>
        services.AddOutboxDispatching<PricingDbContext>(typeof(ChannelPriceChanged).Assembly);
}
