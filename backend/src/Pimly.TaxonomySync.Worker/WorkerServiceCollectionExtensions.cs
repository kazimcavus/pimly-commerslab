using Channels.Application;
using Channels.Infrastructure;
using Pimly.TaxonomySync.Worker.Taxonomy;
using SharedKernel.Tenancy;

namespace Pimly.TaxonomySync.Worker;

/// <summary>
/// Worker servis kompozisyonu. Program.cs dışında tutulur ki entegrasyon testleri
/// aynı kompozisyonu kendi host'larında kurabilsin. Tenant bazlı ürün import'u
/// ayrı host'ta (Pimly.ProductImports.Worker) çalışır; burada yalnızca
/// tenant-agnostik taxonomy senkron servisleri kalır.
/// </summary>
public static class WorkerServiceCollectionExtensions
{
    /// <summary>Worker'ın tüm modül ve servis kayıtlarını yapar.</summary>
    public static IServiceCollection AddPimlyTaxonomySyncWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddChannelsApplication();
        services.AddChannelsInfrastructure(configuration);

        // Worker HTTP bağlamı olmadığı için tenant, iş başına elle set edilen ambient bağlamdan akar.
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());

        services.AddHostedService<TaxonomySyncBackgroundService>();
        services.AddHostedService<ScheduledTaxonomySyncBackgroundService>();

        return services;
    }
}
