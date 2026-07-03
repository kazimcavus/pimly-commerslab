using Catalog.Domain.Barcodes;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Catalog.Domain.SkuGenerator;
using Catalog.Domain.Variants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Tenancy;
using CatalogVariant = Catalog.Domain.Variants.Variant;
using DomainAttribute = Catalog.Domain.Attributes.Attribute;

namespace Catalog.Infrastructure.Tenancy;

/// <summary>Catalog EF modeline tenant shadow property ve query filter ekler.</summary>
internal static class CatalogTenancyExtensions
{
    public static void ApplyCatalogTenancy(this ModelBuilder modelBuilder, Guid tenantId)
    {
        ConfigureTenantRoot<Category>(modelBuilder, tenantId);
        ConfigureTenantRoot<DomainAttribute>(modelBuilder, tenantId);
        ConfigureTenantRoot<CatalogVariant>(modelBuilder, tenantId);
        ConfigureTenantRoot<Product>(modelBuilder, tenantId);
        ConfigureTenantRoot<ProductItem>(modelBuilder, tenantId);
        ConfigureTenantRoot<ProductImage>(modelBuilder, tenantId);
        ConfigureTenantRoot<ProductItemChannelPrice>(modelBuilder, tenantId);
        ConfigureTenantRoot<BarcodeSequence>(modelBuilder, tenantId);
        ConfigureTenantRoot<BarcodeAllocation>(modelBuilder, tenantId);
        ConfigureTenantRoot<SkuGeneratorConfig>(modelBuilder, tenantId);
    }

    public static void StampTenantId(this DbContext db, Guid tenantId)
    {
        foreach (var entry in db.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            if (entry.Metadata.FindProperty(TenantEntityShadowProperty.Name) is null)
            {
                continue;
            }

            if (tenantId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Tenant id is required to persist tenant-scoped catalog data.");
            }

            entry.Property(TenantEntityShadowProperty.Name).CurrentValue = tenantId;
        }
    }

    private static void ConfigureTenantRoot<TEntity>(ModelBuilder modelBuilder, Guid tenantId)
        where TEntity : class
    {
        var builder = modelBuilder.Entity<TEntity>();
        AddTenantShadowProperty(builder);

        if (tenantId == Guid.Empty)
        {
            return;
        }

        builder.HasQueryFilter(entity =>
            EF.Property<Guid>(entity, TenantEntityShadowProperty.Name) == tenantId);
    }

    private static void AddTenantShadowProperty<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<Guid>(TenantEntityShadowProperty.Name)
            .HasColumnName("tenant_id")
            .IsRequired();
    }
}
