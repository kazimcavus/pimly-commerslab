using Channels.Application.Options;
using Channels.Application.Taxonomy;
using Channels.Domain;
using Channels.Domain.Connections;
using Channels.Domain.Taxonomy;
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

        if (options.UseStubTaxonomyClient)
        {
            services.AddScoped<IMarketplaceTaxonomyClient, StubMarketplaceTaxonomyClient>();
            services.AddScoped<IMarketplaceCategoryAttributesClient, StubMarketplaceCategoryAttributesClient>();
            return;
        }

        services.AddScoped<IMarketplaceTaxonomyClient, TrendyolMarketplaceTaxonomyClient>();
        services.AddScoped<IMarketplaceCategoryAttributesClient, TrendyolMarketplaceCategoryAttributesClient>();
    }
}
