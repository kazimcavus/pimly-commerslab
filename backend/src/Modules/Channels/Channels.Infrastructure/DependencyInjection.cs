using Channels.Application.ExternalCatalog;
using Channels.Application.Options;
using Channels.Application.ProductImports;
using Channels.Domain;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.Connections;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.ProductImports;
using Channels.Domain.Publications;
using Channels.Domain.TaxonomySync;
using Channels.Infrastructure.Options;
using Channels.Infrastructure.Persistence;
using Channels.Infrastructure.ProductImports;
using Channels.Infrastructure.Taxonomy;
using Channels.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Channels.Infrastructure;

/// <summary>Channels altyapı servislerini DI konteynerine kaydeder.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddChannelsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddDbContext<ChannelsDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "channels"))
                .ReplaceService<IModelCacheKeyFactory, ChannelsTenantModelCacheKeyFactory>());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ChannelsDbContext>());
        services.AddScoped<IMarketplaceConnectionRepository, Repositories.MarketplaceConnectionRepository>();
        services.AddScoped<ITaxonomySyncRunRepository, Repositories.TaxonomySyncRunRepository>();
        services.AddScoped<IProductImportRunRepository, Repositories.ProductImportRunRepository>();
        services.AddScoped<IProductPublicationRunRepository, Repositories.ProductPublicationRunRepository>();
        services.AddScoped<IExternalCategoryRepository, Repositories.ExternalCategoryRepository>();
        services.AddScoped<ICategoryChannelMappingRepository, Repositories.CategoryChannelMappingRepository>();
        services.AddScoped<IExternalCategoryAttributeRepository, Repositories.ExternalCategoryAttributeRepository>();
        services.AddScoped<IExternalAttributeValueRepository, Repositories.ExternalAttributeValueRepository>();
        services.AddScoped<IAttributeChannelMappingRepository, Repositories.AttributeChannelMappingRepository>();
        services.AddScoped<IAttributeValueChannelMappingRepository, Repositories.AttributeValueChannelMappingRepository>();

        services.Configure<ChannelsOptions>(configuration.GetSection(ChannelsOptions.SectionName));
        services.Configure<ProductImportOptions>(configuration.GetSection(ProductImportOptions.SectionName));
        services.Configure<TaxonomySyncScheduleOptions>(
            configuration.GetSection(TaxonomySyncScheduleOptions.SectionName));
        AddMarketplaceClients(services, configuration);

        return services;
    }

    private static void AddMarketplaceClients(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(ChannelsOptions.SectionName).Get<ChannelsOptions>()
            ?? new ChannelsOptions();

        services.AddHttpClient(nameof(TrendyolMarketplaceTaxonomyClient));
        services.AddScoped<IMarketplaceTaxonomyClientResolver, MarketplaceTaxonomyClientResolver>();
        services.AddScoped<IMarketplaceCategoryAttributesClientResolver, MarketplaceCategoryAttributesClientResolver>();
        services.AddScoped<IMarketplaceProductsClientResolver, MarketplaceProductsClientResolver>();

        if (options.UseStubTaxonomyClient)
        {
            RegisterKeyedClient<IMarketplaceTaxonomyClient, StubMarketplaceTaxonomyClient>(services);
            RegisterKeyedClient<IMarketplaceCategoryAttributesClient, StubMarketplaceCategoryAttributesClient>(services);
            RegisterKeyedClient<IMarketplaceProductsClient, StubMarketplaceProductsClient>(services);
            return;
        }

        RegisterKeyedClient<IMarketplaceTaxonomyClient, TrendyolMarketplaceTaxonomyClient>(services, Marketplace.Trendyol);
        RegisterKeyedClient<IMarketplaceCategoryAttributesClient, TrendyolMarketplaceCategoryAttributesClient>(services, Marketplace.Trendyol);
        RegisterKeyedClient<IMarketplaceProductsClient, TrendyolMarketplaceProductsClient>(services, Marketplace.Trendyol);
    }

    private static void RegisterKeyedClient<TService, TClient>(
        IServiceCollection services,
        Marketplace? marketplace = null)
        where TService : class
        where TClient : class, TService
    {
        if (marketplace is null)
        {
            foreach (var entry in Marketplace.AllSupported)
            {
                services.AddKeyedScoped<TService, TClient>(entry.Code);
            }

            return;
        }

        services.AddKeyedScoped<TService, TClient>(marketplace.Code);
    }
}
