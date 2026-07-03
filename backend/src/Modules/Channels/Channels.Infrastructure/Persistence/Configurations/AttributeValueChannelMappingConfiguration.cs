using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Channels.Infrastructure.Persistence.Configurations;

internal sealed class AttributeValueChannelMappingConfiguration : IEntityTypeConfiguration<AttributeValueChannelMapping>
{
    public void Configure(EntityTypeBuilder<AttributeValueChannelMapping> builder)
    {
        builder.ToTable("attribute_value_channel_mappings");

        builder.HasKey(mapping => mapping.Id);
        builder.Property(mapping => mapping.Id).HasColumnName("id");
        builder.Ignore(mapping => mapping.DomainEvents);

        builder.Property(mapping => mapping.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(mapping => mapping.AttributeChannelMappingId)
            .HasColumnName("attribute_channel_mapping_id")
            .IsRequired();

        builder.Property(mapping => mapping.CatalogValueId)
            .HasColumnName("catalog_value_id")
            .IsRequired();

        builder.Property(mapping => mapping.ExternalValueId)
            .HasColumnName("external_value_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(mapping => new { mapping.AttributeChannelMappingId, mapping.CatalogValueId }).IsUnique();
    }
}
