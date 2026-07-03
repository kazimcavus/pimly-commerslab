using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        var keyProperty = builder.Property(value => value.MarketplaceKey)
            .HasColumnName("marketplace_key")
            .HasConversion(key => key.Value, val => MarketplaceKey.FromPersistence(val))
            .HasMaxLength(MarketplaceKey.MaxLength)
            .IsRequired();

        keyProperty.Metadata.SetValueComparer(new ValueComparer<MarketplaceKey>(
            (left, right) => left!.Value == right!.Value,
            key => key.Value.GetHashCode(StringComparison.Ordinal),
            key => MarketplaceKey.FromPersistence(key.Value)));

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
            value.MarketplaceKey,
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
