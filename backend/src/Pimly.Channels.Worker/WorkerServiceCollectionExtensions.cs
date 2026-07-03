using Catalog.Application;
using Catalog.Infrastructure;
using Channels.Application;
using Channels.Application.Imports.ProcessProductImport;
using Channels.Application.Ports;
using Channels.Infrastructure;
using Media.Application;
using Media.Infrastructure;
using Pimly.Channels.Worker.Imports;
using Pimly.Channels.Worker.Integration;
using Pimly.Channels.Worker.Taxonomy;
using SharedKernel.Tenancy;

namespace Pimly.Channels.Worker;

/// <summary>
/// Worker servis kompozisyonu. Program.cs dışında tutulur ki entegrasyon testleri
/// aynı kompozisyonu kendi host'larında kurabilsin.
/// </summary>
public static class WorkerServiceCollectionExtensions
{
    /// <summary>Worker'ın tüm modül ve servis kayıtlarını yapar.</summary>
    public static IServiceCollection AddPimlyWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddChannelsApplication();
        services.AddChannelsInfrastructure(configuration);

        // Ürün import'u Catalog'a yazar ve görselleri Media'ya alır.
        services.AddCatalogApplication();
        services.AddCatalogInfrastructure(configuration);
        services.AddMediaApplication();
        services.AddMediaInfrastructure(configuration);

        // Worker HTTP bağlamı olmadığı için tenant, iş başına elle set edilen ambient bağlamdan akar.
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());

        // Görsel indirme tek iş parçacığında; yavaş bir CDN yanıtı tüm import'u bloklamasın.
        services.AddHttpClient(nameof(CatalogImportGateway), client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddScoped<ICatalogImportGateway, CatalogImportGateway>();

        // İşlemci, Catalog yazma kapısına ihtiyaç duyduğu için yalnızca worker kompozisyonunda kayıtlıdır.
        services.AddScoped<IProcessProductImportHandler, ProcessProductImportHandler>();

        services.AddHostedService<TaxonomySyncBackgroundService>();
        services.AddHostedService<ScheduledTaxonomySyncBackgroundService>();
        services.AddHostedService<ProductImportBackgroundService>();

        return services;
    }
}
