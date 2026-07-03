using Channels.Application.ExternalCatalog;
using Channels.Application.Options;
using Channels.Domain;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.Connections;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.Marketplaces;
using Channels.Domain.TaxonomySync;
using Channels.Infrastructure.Options;
using Channels.Infrastructure.Persistence;
using Channels.Infrastructure.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "channels")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ChannelsDbContext>());
        services.AddScoped<IMarketplaceConnectionRepository, Repositories.MarketplaceConnectionRepository>();
        services.AddScoped<ITaxonomySyncRunRepository, Repositories.TaxonomySyncRunRepository>();
        services.AddScoped<IExternalCategoryRepository, Repositories.ExternalCategoryRepository>();
        services.AddScoped<ICategoryChannelMappingRepository, Repositories.CategoryChannelMappingRepository>();
        services.AddScoped<IExternalCategoryAttributeRepository, Repositories.ExternalCategoryAttributeRepository>();
        services.AddScoped<IExternalAttributeValueRepository, Repositories.ExternalAttributeValueRepository>();
        services.AddScoped<IAttributeChannelMappingRepository, Repositories.AttributeChannelMappingRepository>();
        services.AddScoped<IAttributeValueChannelMappingRepository, Repositories.AttributeValueChannelMappingRepository>();

        services.Configure<ChannelsOptions>(configuration.GetSection(ChannelsOptions.SectionName));
        services.Configure<TaxonomySyncScheduleOptions>(
            configuration.GetSection(TaxonomySyncScheduleOptions.SectionName));
        AddTaxonomyClient(services, configuration);

        return services;
    }

    private static void AddTaxonomyClient(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(ChannelsOptions.SectionName).Get<ChannelsOptions>()
            ?? new ChannelsOptions();

        services.AddHttpClient(nameof(TrendyolMarketplaceTaxonomyClient));
        services.AddScoped<IMarketplaceTaxonomyClientResolver, MarketplaceTaxonomyClientResolver>();
        services.AddScoped<IMarketplaceCategoryAttributesClientResolver, MarketplaceCategoryAttributesClientResolver>();

        if (options.UseStubTaxonomyClient)
        {
            RegisterTaxonomyClient<StubMarketplaceTaxonomyClient>(services);
            RegisterCategoryAttributesClient<StubMarketplaceCategoryAttributesClient>(services);
            return;
        }

        RegisterTaxonomyClient<TrendyolMarketplaceTaxonomyClient>(services, Marketplace.Trendyol);
        RegisterCategoryAttributesClient<TrendyolMarketplaceCategoryAttributesClient>(services, Marketplace.Trendyol);
    }

    private static void RegisterTaxonomyClient<TClient>(
        IServiceCollection services,
        Marketplace? marketplace = null)
        where TClient : class, IMarketplaceTaxonomyClient
    {
        if (marketplace is null)
        {
            foreach (var entry in Marketplace.AllSupported)
            {
                services.AddKeyedScoped<IMarketplaceTaxonomyClient, TClient>(entry.Code);
            }

            return;
        }

        services.AddKeyedScoped<IMarketplaceTaxonomyClient, TClient>(marketplace.Code);
    }

    private static void RegisterCategoryAttributesClient<TClient>(
        IServiceCollection services,
        Marketplace? marketplace = null)
        where TClient : class, IMarketplaceCategoryAttributesClient
    {
        if (marketplace is null)
        {
            foreach (var entry in Marketplace.AllSupported)
            {
                services.AddKeyedScoped<IMarketplaceCategoryAttributesClient, TClient>(entry.Code);
            }

            return;
        }

        services.AddKeyedScoped<IMarketplaceCategoryAttributesClient, TClient>(marketplace.Code);
    }
}
