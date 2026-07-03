using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Channels.Infrastructure.Persistence.Configurations;

internal sealed class ExternalAttributeValueConfiguration : IEntityTypeConfiguration<ExternalAttributeValue>
{
    public void Configure(EntityTypeBuilder<ExternalAttributeValue> builder)
    {
        builder.ToTable("external_attribute_values");

        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).HasColumnName("id");
        builder.Ignore(value => value.DomainEvents);

        builder.Property(value => value.Marketplace)
            .ConfigureMarketplaceColumn();

        builder.Property(value => value.ExternalCategoryId)
            .HasColumnName("external_category_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(value => value.ExternalAttributeId)
            .HasColumnName("external_attribute_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(value => value.ExternalValueId)
            .HasColumnName("external_value_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(value => new
        {
            value.Marketplace,
            value.ExternalCategoryId,
            value.ExternalAttributeId,
            value.ExternalValueId,
        }).IsUnique();

        builder.Property(value => value.Name)
            .HasColumnName("name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(value => value.SyncedAt)
            .HasColumnName("synced_at")
            .IsRequired();
    }
}
