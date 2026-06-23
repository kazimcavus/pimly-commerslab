using Catalog.Domain;
using Catalog.Domain.Barcodes;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Catalog.Domain.Variants;
using Microsoft.EntityFrameworkCore;
using CatalogVariant = Catalog.Domain.Variants.Variant;
using DomainAttribute = Catalog.Domain.Attributes.Attribute;

namespace Catalog.Infrastructure.Persistence;

/// <summary>Catalog modülü için Entity Framework veritabanı bağlamı.</summary>
public sealed class CatalogDbContext : DbContext, IUnitOfWork
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<DomainAttribute> Attributes => Set<DomainAttribute>();

    public DbSet<CatalogVariant> Variants => Set<CatalogVariant>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductItem> ProductItems => Set<ProductItem>();

    public DbSet<BarcodeSequence> BarcodeSequences => Set<BarcodeSequence>();

    public DbSet<BarcodeAllocation> BarcodeAllocations => Set<BarcodeAllocation>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
