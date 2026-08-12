using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.Connections;
using Channels.Domain.Listings;
using Channels.Domain.ProductImports;
using Microsoft.EntityFrameworkCore;

namespace Channels.Infrastructure.Tenancy;

/// <summary>Channels EF modeline tenant query filter ekler.</summary>
internal static class ChannelsTenancyExtensions
{
    public static void ApplyChannelsTenancy(this ModelBuilder modelBuilder, Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            return;
        }

        modelBuilder.Entity<MarketplaceConnection>()
            .HasQueryFilter(connection => connection.TenantId == tenantId);

        // Worker tenant'sız çalıştığı için (tenantId == Guid.Empty) kuyruk claim'i filtreden etkilenmez;
        // API tarafında ise run'lar yalnızca sahibi tenant'a görünür.
        modelBuilder.Entity<ProductImportRun>()
            .HasQueryFilter(run => run.TenantId == tenantId);

        modelBuilder.Entity<CategoryChannelMapping>()
            .HasQueryFilter(mapping => mapping.TenantId == tenantId);

        modelBuilder.Entity<AttributeChannelMapping>()
            .HasQueryFilter(mapping => mapping.TenantId == tenantId);

        modelBuilder.Entity<AttributeValueChannelMapping>()
            .HasQueryFilter(mapping => mapping.TenantId == tenantId);

        // Senkron worker'ı kirli (tenant, pazaryeri) çiftlerini keşfederken bu filtreyi bilinçli
        // olarak IgnoreQueryFilters ile aşar; işleme her zaman tenant bağlamı kurulduktan sonra yapılır.
        modelBuilder.Entity<ProductListing>()
            .HasQueryFilter(listing => listing.TenantId == tenantId);
    }
}
