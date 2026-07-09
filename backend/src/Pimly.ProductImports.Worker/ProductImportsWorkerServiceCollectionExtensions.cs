using Catalog.Application;
using Catalog.Infrastructure;
using Channels.Application;
using Channels.Application.ProductImports.Catalog;
using Channels.Application.ProductImports.ProcessProductImport;
using Channels.Infrastructure;
using Media.Application;
using Media.Infrastructure;
using Pimly.ProductImports.Worker.Integration;
using Pimly.ProductImports.Worker.Options;
using Pimly.ProductImports.Worker.ProductImports;
using Pricing.Application;
using Pricing.Application.ItemPrices.Catalog;
using Pricing.Infrastructure;
using SharedKernel.Tenancy;

namespace Pimly.ProductImports.Worker;

/// <summary>
/// Ürün import worker'ının servis kompozisyonu. Program.cs dışında tutulur ki entegrasyon
/// testleri aynı kompozisyonu kendi host'larında kurabilsin.
/// </summary>
public static class ProductImportsWorkerServiceCollectionExtensions
{
    /// <summary>Worker'ın tüm modül ve servis kayıtlarını yapar.</summary>
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
        services.AddMediaApplication();
        services.AddMediaInfrastructure(configuration);

        // Pricing'in kalem fiyatı yazımı, kalemin Catalog'da var olduğunu bu ACL portuyla doğrular.
        services.AddScoped<ICatalogProductItemGateway, CatalogProductItemGateway>();

        // Worker HTTP bağlamı olmadığı için tenant, iş başına elle set edilen ambient bağlamdan akar.
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());

        // TenantIds zorunlu: worker hangi tenant'lara hizmet ettiğini açıkça bildirmeden başlayamaz.
        services.AddOptions<ProductImportsWorkerOptions>()
            .Bind(configuration.GetSection(ProductImportsWorkerOptions.SectionName))
            .Validate(
                options => options.TenantIds.Count > 0,
                "ProductImports:TenantIds boş olamaz; worker'ın hizmet edeceği tenant'lar açıkça belirtilmelidir.")
            .Validate(
                options => options.TenantIds.All(id => id != Guid.Empty),
                "ProductImports:TenantIds boş GUID içeremez.")
            .ValidateOnStart();

        // Görsel indirme tek iş parçacığında; yavaş bir CDN yanıtı tüm import'u bloklamasın.
        services.AddHttpClient(nameof(CatalogImportGateway), client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddScoped<ICatalogImportGateway, CatalogImportGateway>();

        // İşlemci, Catalog yazma kapısına ihtiyaç duyduğu için yalnızca worker kompozisyonunda kayıtlıdır.
        services.AddScoped<IProcessProductImportHandler, ProcessProductImportHandler>();

        services.AddHostedService<ProductImportBackgroundService>();

        return services;
    }
}
