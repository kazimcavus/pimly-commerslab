using Catalog.Application;
using Catalog.Infrastructure;
using Channels.Application;
using Channels.Application.Publications.ProcessPublication;
using Channels.Infrastructure;
using Inventory.Application;
using Inventory.Infrastructure;
using Pimly.Integration;
using Pimly.ProductPublications.Worker.Options;
using Pimly.ProductPublications.Worker.Publications;
using Pricing.Application;
using Pricing.Infrastructure;
using SharedKernel.Tenancy;

namespace Pimly.ProductPublications.Worker;

/// <summary>
/// Ürün yayın (publish) worker'ının servis kompozisyonu. Program.cs dışında tutulur ki entegrasyon
/// testleri aynı kompozisyonu kendi host'larında kurabilsin.
/// </summary>
/// <remarks>
/// Kapsam bilinçli olarak dardır: yayın yalnızca Channels'ın yayın kuyruğuna ve Pricing'in
/// kararlaştırılmış kanal fiyatlarına ihtiyaç duyar — Catalog/Media yazma kapılarına değil.
/// </remarks>
public static class ProductPublicationsWorkerServiceCollectionExtensions
{
    /// <summary>Worker'ın tüm modül ve servis kayıtlarını yapar.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="configuration">Uygulama konfigürasyonu.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddPimlyProductPublicationsWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddChannelsApplication();
        services.AddChannelsInfrastructure(configuration);
        services.AddPricingApplication();
        services.AddPricingInfrastructure(configuration);

        // Ürün kartı payload'ı Catalog içeriğinden kurulur, stok Inventory'den okunur.
        services.AddCatalogApplication();
        services.AddCatalogInfrastructure(configuration);
        services.AddInventoryApplication();
        services.AddInventoryInfrastructure(configuration);

        // Worker HTTP bağlamı olmadığı için tenant, iş başına elle set edilen ambient bağlamdan akar.
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());

        // TenantIds opsiyonel: boş liste tüm tenant'ların run'larının claim edilmesi demektir;
        // tenant-izole instance çalıştırmak isteyen dağıtımlar listeyi doldurur.
        services.AddOptions<ProductPublicationsWorkerOptions>()
            .Bind(configuration.GetSection(ProductPublicationsWorkerOptions.SectionName))
            .Validate(
                options => options.TenantIds.All(id => id != Guid.Empty),
                "ProductPublications:TenantIds boş GUID içeremez.")
            .ValidateOnStart();

        // Yayın payload'ı: Catalog'dan içerik, Pricing'den kanal fiyatı, Inventory'den stok.
        services.AddCatalogListingSourceGateway();
        services.AddPricingChannelPriceGateway();
        services.AddInventoryStockGateway();
        services.AddScoped<IProcessPublicationHandler, ProcessPublicationHandler>();

        services.AddHostedService<ProductPublicationBackgroundService>();

        return services;
    }
}
