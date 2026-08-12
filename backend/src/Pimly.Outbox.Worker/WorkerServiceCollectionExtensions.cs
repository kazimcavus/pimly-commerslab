using Catalog.Domain.Products.Events;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Outbox;
using Catalog.Infrastructure.Persistence;
using Channels.Application;
using Channels.Infrastructure;
using Inventory.Application;
using Inventory.Domain.StockLevels.Events;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Outbox;
using Inventory.Infrastructure.Persistence;
using Pricing.Application;
using Pricing.Domain.ChannelPrices.Events;
using Pricing.Infrastructure;
using Pricing.Infrastructure.Outbox;
using Pricing.Infrastructure.Persistence;
using SharedKernel;
using SharedKernel.Tenancy;

namespace Pimly.Outbox.Worker;

/// <summary>
/// Catalog outbox dispatcher worker'ının servis kompozisyonu. Program.cs dışında tutulur
/// ki entegrasyon testleri aynı kompozisyonu kendi host'unda kurabilsin.
/// </summary>
public static class WorkerServiceCollectionExtensions
{
    /// <summary>Worker'ın tüm servis kayıtlarını yapar.</summary>
    public static IServiceCollection AddPimlyOutboxWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCatalogInfrastructure(configuration);

        // Pricing ve Inventory, kalem silindiğinde ilgili fiyat/stok kayıtlarını temizlemek için dinler.
        services.AddPricingApplication();
        services.AddPricingInfrastructure(configuration);
        services.AddInventoryApplication();
        services.AddInventoryInfrastructure(configuration);

        // Channels, fiyat/stok değişimlerini listeleme kirliliğine çevirmek için dinler.
        services.AddChannelsApplication();
        services.AddChannelsInfrastructure(configuration);

        // HTTP bağlamı yok: tenant, iş başına elle set edilen ambient bağlamdan akar.
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());

        services.AddCatalogOutboxDispatching();
        services.AddPricingOutboxDispatching();
        services.AddInventoryOutboxDispatching();

        // İskelet kanıtı: gerçek Pricing/Inventory handler'ları gelene kadar loglayan subscriber.
        services.AddScoped<IIntegrationEventHandler<ProductItemCreated>, ProductItemCreatedLoggingHandler>();

        // Kalem silindiğinde Pricing'deki fiyatları ve Inventory'deki stoğu temizler (birden çok subscriber).
        services.AddScoped<IIntegrationEventHandler<ProductItemDeleted>, ProductItemDeletedPricingHandler>();
        services.AddScoped<IIntegrationEventHandler<ProductItemDeleted>, ProductItemDeletedInventoryHandler>();

        // Fiyat/stok değişimi → ilgili listelemeler "teklif kirli". Pazaryerine burada çağrı YAPILMAZ;
        // gönderimi listing senkron worker'ı toplu ve debounce edilmiş şekilde üstlenir.
        services.AddScoped<IIntegrationEventHandler<StockLevelChanged>, StockLevelChangedListingHandler>();
        services.AddScoped<IIntegrationEventHandler<ChannelPriceChanged>, ChannelPriceChangedListingHandler>();

        // İçerik değişimi → ilgili listelemeler "içerik kirli" (pahalı uç, yeniden onaya girer).
        services.AddScoped<IIntegrationEventHandler<ProductContentChanged>, ProductContentChangedListingHandler>();

        services.AddHostedService<OutboxDispatcherBackgroundService<CatalogDbContext>>();
        services.AddHostedService<OutboxDispatcherBackgroundService<PricingDbContext>>();
        services.AddHostedService<OutboxDispatcherBackgroundService<InventoryDbContext>>();

        return services;
    }
}
