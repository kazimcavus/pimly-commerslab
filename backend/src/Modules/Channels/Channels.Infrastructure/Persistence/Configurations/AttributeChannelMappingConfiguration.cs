using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Channels.Infrastructure.Persistence.Configurations;

internal sealed class AttributeChannelMappingConfiguration : IEntityTypeConfiguration<AttributeChannelMapping>
{
    public void Configure(EntityTypeBuilder<AttributeChannelMapping> builder)
    {
        builder.ToTable("attribute_channel_mappings");

        builder.HasKey(mapping => mapping.Id);
        builder.Property(mapping => mapping.Id).HasColumnName("id");
        builder.Ignore(mapping => mapping.DomainEvents);

        builder.Property(mapping => mapping.TenantId)
            .HasColumnName("tenant_id")
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

        builder.Property(mapping => mapping.CatalogCategoryId)
            .HasColumnName("catalog_category_id")
            .IsRequired();

        builder.Property(mapping => mapping.SourceType)
            .HasColumnName("source_type")
            .HasConversion(
                sourceType => ToPersistence(sourceType),
                value => FromPersistence(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(mapping => mapping.CatalogSourceId)
            .HasColumnName("catalog_source_id")
            .IsRequired();

        builder.Property(mapping => mapping.ExternalAttributeId)
            .HasColumnName("external_attribute_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(mapping => new
        {
            mapping.TenantId,
            mapping.MarketplaceKey,
            mapping.CatalogCategoryId,
            mapping.SourceType,
            mapping.CatalogSourceId,
        }).IsUnique();

        builder.HasIndex(mapping => new { mapping.TenantId, mapping.MarketplaceKey, mapping.CatalogCategoryId });
    }

    private static string ToPersistence(AttributeMappingSourceType sourceType) =>
        sourceType switch
        {
            AttributeMappingSourceType.CatalogAttribute => "catalog_attribute",
            AttributeMappingSourceType.CatalogVariant => "catalog_variant",
            _ => throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, null),
        };

    private static AttributeMappingSourceType FromPersistence(string value) =>
        value switch
        {
            "catalog_attribute" => AttributeMappingSourceType.CatalogAttribute,
            "catalog_variant" => AttributeMappingSourceType.CatalogVariant,
            _ => AttributeMappingSourceType.CatalogAttribute,
        };
}
