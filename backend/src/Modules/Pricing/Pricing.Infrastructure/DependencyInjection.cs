using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pricing.Domain;
using Pricing.Domain.BasePrices;
using Pricing.Domain.ItemPrices;
using Pricing.Domain.PriceDefinitions;
using Pricing.Infrastructure.Persistence;
using Pricing.Infrastructure.Repositories;
using Pricing.Infrastructure.Tenancy;

namespace Pricing.Infrastructure;

/// <summary>Pricing altyapı servislerini DI konteynerine kaydeder.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPricingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddDbContext<PricingDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "pricing"))
                .ReplaceService<IModelCacheKeyFactory, PricingTenantModelCacheKeyFactory>());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PricingDbContext>());
        services.AddScoped<IPriceDefinitionRepository, PriceDefinitionRepository>();
        services.AddScoped<IItemPriceRepository, ItemPriceRepository>();
        services.AddScoped<IBasePriceRepository, BasePriceRepository>();

        return services;
    }
}
