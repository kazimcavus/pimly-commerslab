using Catalog.Application;
using Catalog.Infrastructure;
using Channels.Application;
using Channels.Application.ProductImports.ProcessProductImport;
using Channels.Infrastructure;
using Inventory.Application;
using Inventory.Infrastructure;
using Media.Application;
using Media.Infrastructure;
using Pimly.Integration;
using Pimly.ProductImports.Worker.Options;
using Pimly.ProductImports.Worker.ProductImports;
using Pricing.Application;
using Pricing.Infrastructure;
using SharedKernel.Tenancy;

namespace Pimly.ProductImports.Worker;

/// <summary>
/// Ürün import worker'ının servis kompozisyonu. Program.cs dışında tutulur ki entegrasyon
/// testleri aynı kompozisyonu kendi host'larında kurabilsin.
/// </summary>
/// <remarks>
/// Kapsam: pazaryerinden ürünleri çekip kataloğa yazmak. Bu yüzden modül ayak izi worker'lar
/// arasında en geniş olanıdır (Catalog + Pricing + Inventory + Media yazma kapıları). Yayın ve
/// listeleme senkronu ayrı host'larda çalışır (Pimly.ProductPublications.Worker, Pimly.ListingSync.Worker).
/// </remarks>
public static class ProductImportsWorkerServiceCollectionExtensions
{
    /// <summary>Worker'ın tüm modül ve servis kayıtlarını yapar.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="configuration">Uygulama konfigürasyonu.</param>
    /// <returns>Zincirleme için aynı servis koleksiyonu.</returns>
    public static IServiceCollection AddPimlyProductImportsWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddChannelsApplication();
        services.AddChannelsInfrastructure(configuration);

        // Ürün import'u Catalog'a (kimlik/tanım) ve Pricing'e (fiyat) yazar, görselleri Media'ya alır.
        services.AddCatalogApplication();
        services.AddCatalogInfrastructure(configuration);
        services.AddPricingApplication();
        services.AddPricingInfrastructure(configuration);
        services.AddInventoryApplication();
        services.AddInventoryInfrastructure(configuration);
        services.AddMediaApplication();
        services.AddMediaInfrastructure(configuration);

        // Pricing ve Inventory, yazmadan önce kalemin Catalog'da var olduğunu bu ACL portlarıyla doğrular.
        services.AddProductItemExistenceGateways();

        // Worker HTTP bağlamı olmadığı için tenant, iş başına elle set edilen ambient bağlamdan akar.
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());

        // TenantIds opsiyonel: boş liste tüm tenant'ların run'larının claim edilmesi demektir;
        // tenant-izole instance çalıştırmak isteyen dağıtımlar listeyi doldurur.
        services.AddOptions<ProductImportsWorkerOptions>()
            .Bind(configuration.GetSection(ProductImportsWorkerOptions.SectionName))
            .Validate(
                options => options.TenantIds.All(id => id != Guid.Empty),
                "ProductImports:TenantIds boş GUID içeremez.")
            .ValidateOnStart();

        // Görsel indirme tek iş parçacığında; yavaş bir CDN yanıtı tüm import'u bloklamasın.
        services.AddHttpClient(nameof(CatalogImportGateway), client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddCatalogImportGateway();

        // İşlemci, Catalog yazma kapısına ihtiyaç duyduğu için yalnızca worker kompozisyonunda kayıtlıdır.
        services.AddScoped<IProcessProductImportHandler, ProcessProductImportHandler>();

        services.AddHostedService<ProductImportBackgroundService>();

        return services;
    }
}
