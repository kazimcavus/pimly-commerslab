using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        var keyProperty = builder.Property(mapping => mapping.MarketplaceKey)
            .HasColumnName("marketplace_key")
            .HasConversion(key => key.Value, value => MarketplaceKey.FromPersistence(value))
            .HasMaxLength(MarketplaceKey.MaxLength)
            .IsRequired();

        keyProperty.Metadata.SetValueComparer(new ValueComparer<MarketplaceKey>(
            (left, right) => left!.Value == right!.Value,
            key => key.Value.GetHashCode(StringComparison.Ordinal),
            key => MarketplaceKey.FromPersistence(key.Value)));

        builder.Property(mapping => mapping.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(mapping => new
        {
            mapping.TenantId,
            mapping.CatalogCategoryId,
            mapping.MarketplaceKey,
        }).IsUnique();

        builder.HasIndex(mapping => new { mapping.TenantId, mapping.MarketplaceKey });
        builder.HasIndex(mapping => new { mapping.TenantId, mapping.MarketplaceKey, mapping.ExternalId });
    }
}
