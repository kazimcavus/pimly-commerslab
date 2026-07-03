using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using Microsoft.EntityFrameworkCore;
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

        builder.Property(mapping => mapping.Marketplace)
            .ConfigureMarketplaceColumn();

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
            mapping.Marketplace,
            mapping.CatalogCategoryId,
            mapping.SourceType,
            mapping.CatalogSourceId,
        }).IsUnique();

        builder.HasIndex(mapping => new { mapping.TenantId, mapping.Marketplace, mapping.CatalogCategoryId });
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
