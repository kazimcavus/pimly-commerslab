using Catalog.Application.Barcodes;
using Catalog.Application.Options;
using Catalog.Application.SkuGenerator;
using Catalog.Domain;
using Catalog.Domain.Products;
using Catalog.Domain.Settings;
using Catalog.Domain.SkuGenerator;
using Catalog.Infrastructure.Barcodes;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Repositories;
using Catalog.Infrastructure.SkuGenerator;
using Catalog.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

/// <summary>Catalog altyapı servislerini DI konteynerine kaydeder.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddDbContext<CatalogDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "catalog"))
                .ReplaceService<IModelCacheKeyFactory, CatalogTenantModelCacheKeyFactory>());

        services.Configure<MediaUrlOptions>(configuration.GetSection(MediaUrlOptions.SectionName));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IAttributeRepository, AttributeRepository>();
        services.AddScoped<IVariantRepository, VariantRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPriceDefinitionRepository, PriceDefinitionRepository>();
        services.AddScoped<IItemPriceRepository, ItemPriceRepository>();
        services.AddScoped<IBarcodeSequenceRepository, BarcodeSequenceRepository>();
        services.AddScoped<IBarcodeAllocationRepository, BarcodeAllocationRepository>();
        services.AddScoped<IBarcodeAllocator, BarcodeAllocator>();
        services.AddScoped<ISkuGeneratorConfigRepository, SkuGeneratorConfigRepository>();
        services.AddScoped<ISkuCounterAllocator, SkuCounterAllocator>();
        services.AddScoped<ICatalogSettingsRepository, CatalogSettingsRepository>();

        return services;
    }
}
