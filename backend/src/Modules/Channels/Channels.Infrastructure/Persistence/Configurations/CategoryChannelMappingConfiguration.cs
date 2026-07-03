using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Channels.Infrastructure.Persistence.Configurations;

/// <summary>CategoryChannelMapping aggregate kökünün EF Core eşleme yapılandırması.</summary>
internal sealed class CategoryChannelMappingConfiguration : IEntityTypeConfiguration<CategoryChannelMapping>
{
    public void Configure(EntityTypeBuilder<CategoryChannelMapping> builder)
    {
        builder.ToTable("category_channel_mappings");

        builder.HasKey(mapping => mapping.Id);
        builder.Property(mapping => mapping.Id).HasColumnName("id");
        builder.Ignore(mapping => mapping.DomainEvents);

        builder.Property(mapping => mapping.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(mapping => mapping.CatalogCategoryId)
            .HasColumnName("catalog_category_id")
            .IsRequired();

        builder.Property(mapping => mapping.Marketplace)
            .ConfigureMarketplaceColumn();

        builder.Property(mapping => mapping.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(mapping => new
        {
            mapping.TenantId,
            mapping.CatalogCategoryId,
            mapping.Marketplace,
        }).IsUnique();

        builder.HasIndex(mapping => new { mapping.TenantId, mapping.Marketplace });
        builder.HasIndex(mapping => new { mapping.TenantId, mapping.Marketplace, mapping.ExternalId });
    }
}
