using Catalog.Domain.Products.Events;
using Catalog.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Pimly.Outbox;

namespace Catalog.Infrastructure.Outbox;

/// <summary>Catalog outbox dağıtım servislerini DI'a kaydeder.</summary>
public static class OutboxDispatchingServiceCollectionExtensions
{
    /// <summary>
    /// Ortak outbox mekanizmasını Catalog DbContext'i için kaydeder. Integration olay tipleri
    /// Catalog.Domain assembly'sinden taranır; handler'lar çağıran tarafça ayrıca kaydedilir.
    /// </summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddCatalogOutboxDispatching(this IServiceCollection services) =>
        services.AddOutboxDispatching<CatalogDbContext>(typeof(ProductItemCreated).Assembly);
}
