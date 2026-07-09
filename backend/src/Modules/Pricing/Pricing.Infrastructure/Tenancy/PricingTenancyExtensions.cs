using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.BasePrices;
using Pricing.Domain.ChannelPrices;
using Pricing.Domain.ItemPrices;
using Pricing.Domain.PriceDefinitions;
using SharedKernel.Tenancy;

namespace Pricing.Infrastructure.Tenancy;

/// <summary>Pricing EF modeline tenant shadow property ve query filter ekler.</summary>
internal static class PricingTenancyExtensions
{
    public static void ApplyPricingTenancy(this ModelBuilder modelBuilder, Guid tenantId)
    {
        ConfigureTenantRoot<PriceDefinition>(modelBuilder, tenantId);
        ConfigureTenantRoot<ProductItemPrice>(modelBuilder, tenantId);
        ConfigureTenantRoot<BasePrice>(modelBuilder, tenantId);
        ConfigureTenantRoot<ChannelPrice>(modelBuilder, tenantId);
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
                    "Tenant id is required to persist tenant-scoped pricing data.");
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
