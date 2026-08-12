using Catalog.Application;
using Catalog.Infrastructure;
using Channels.Application;
using Channels.Infrastructure;
using Inventory.Application;
using Inventory.Infrastructure;
using Pimly.Integration;
using Pimly.ListingSync.Worker.Listings;
using Pimly.ListingSync.Worker.Options;
using Pricing.Application;
using Pricing.Infrastructure;
using SharedKernel.Tenancy;

namespace Pimly.ListingSync.Worker;

/// <summary>
/// Listeleme senkron worker'ının servis kompozisyonu. Program.cs dışında tutulur ki entegrasyon
/// testleri aynı kompozisyonu kendi host'larında kurabilsin.
/// </summary>
/// <remarks>
/// Kapsam: canlı listelemelerin fiyat/stok bilgisini pazaryerine taşır. Bunun için Channels
/// (listeleme durumu), Pricing (kanal fiyatı) ve Inventory (stok) yeter — Catalog/Media gerekmez.
/// </remarks>
public static class ListingSyncWorkerServiceCollectionExtensions
{
    /// <summary>Worker'ın tüm modül ve servis kayıtlarını yapar.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="configuration">Uygulama konfigürasyonu.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddPimlyListingSyncWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddChannelsApplication();
        services.AddChannelsInfrastructure(configuration);
        services.AddPricingApplication();
        services.AddPricingInfrastructure(configuration);
        services.AddInventoryApplication();
        services.AddInventoryInfrastructure(configuration);

        // İçerik senkronu için ürün metni/görselleri Catalog'dan okunur.
        services.AddCatalogApplication();
        services.AddCatalogInfrastructure(configuration);

        // Worker HTTP bağlamı olmadığı için tenant, iş başına elle set edilen ambient bağlamdan akar.
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());

        // TenantIds zorunlu: worker hangi tenant'lara hizmet ettiğini açıkça bildirmeden başlayamaz.
        services.AddOptions<ListingSyncWorkerOptions>()
            .Bind(configuration.GetSection(ListingSyncWorkerOptions.SectionName))
            .Validate(
                options => options.TenantIds.Count > 0,
                "ListingSync:TenantIds boş olamaz; worker'ın hizmet edeceği tenant'lar açıkça belirtilmelidir.")
            .Validate(
                options => options.TenantIds.All(id => id != Guid.Empty),
                "ListingSync:TenantIds boş GUID içeremez.")
            .ValidateOnStart();

        // Teklif payload'ı Pricing'den fiyat, Inventory'den stok, içerik payload'ı Catalog'dan kurulur.
        services.AddPricingChannelPriceGateway();
        services.AddInventoryStockGateway();
        services.AddCatalogListingSourceGateway();

        services.AddHostedService<ListingSyncBackgroundService>();

        return services;
    }
}
