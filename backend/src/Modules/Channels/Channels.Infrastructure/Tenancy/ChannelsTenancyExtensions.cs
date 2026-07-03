using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.Connections;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
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

        modelBuilder.Entity<CategoryChannelMapping>()
            .HasQueryFilter(mapping => mapping.TenantId == tenantId);

        modelBuilder.Entity<AttributeChannelMapping>()
            .HasQueryFilter(mapping => mapping.TenantId == tenantId);

        modelBuilder.Entity<AttributeValueChannelMapping>()
            .HasQueryFilter(mapping => mapping.TenantId == tenantId);
    }
}
