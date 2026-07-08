using Catalog.Domain.Products.Events;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Outbox;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Pimly.Catalog.Worker;

/// <summary>
/// Catalog outbox dispatcher worker'ının servis kompozisyonu. Program.cs dışında tutulur
/// ki entegrasyon testleri aynı kompozisyonu kendi host'unda kurabilsin.
/// </summary>
public static class WorkerServiceCollectionExtensions
{
    /// <summary>Worker'ın tüm servis kayıtlarını yapar.</summary>
    public static IServiceCollection AddCatalogOutboxWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCatalogInfrastructure(configuration);

        // HTTP bağlamı yok: tenant, iş başına elle set edilen ambient bağlamdan akar.
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());

        services.AddCatalogOutboxDispatching();

        // İskelet kanıtı: gerçek Pricing/Inventory handler'ları gelene kadar loglayan subscriber.
        services.AddScoped<IIntegrationEventHandler<ProductItemCreated>, ProductItemCreatedLoggingHandler>();

        services.AddHostedService<OutboxDispatcherBackgroundService>();

        return services;
    }
}
