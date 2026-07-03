using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Channels.Infrastructure.Persistence.Configurations;

internal sealed class ExternalCategoryAttributeConfiguration : IEntityTypeConfiguration<ExternalCategoryAttribute>
{
    public void Configure(EntityTypeBuilder<ExternalCategoryAttribute> builder)
    {
        builder.ToTable("external_category_attributes");

        builder.HasKey(attribute => attribute.Id);
        builder.Property(attribute => attribute.Id).HasColumnName("id");
        builder.Ignore(attribute => attribute.DomainEvents);

        var keyProperty = builder.Property(attribute => attribute.MarketplaceKey)
            .HasColumnName("marketplace_key")
            .HasConversion(key => key.Value, value => MarketplaceKey.FromPersistence(value))
            .HasMaxLength(MarketplaceKey.MaxLength)
            .IsRequired();

        keyProperty.Metadata.SetValueComparer(new ValueComparer<MarketplaceKey>(
            (left, right) => left!.Value == right!.Value,
            key => key.Value.GetHashCode(StringComparison.Ordinal),
            key => MarketplaceKey.FromPersistence(key.Value)));

        builder.Property(attribute => attribute.ExternalCategoryId)
            .HasColumnName("external_category_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(attribute => attribute.ExternalAttributeId)
            .HasColumnName("external_attribute_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(attribute => new
        {
            attribute.MarketplaceKey,
            attribute.ExternalCategoryId,
            attribute.ExternalAttributeId,
        }).IsUnique();

        builder.HasIndex(attribute => new { attribute.MarketplaceKey, attribute.ExternalCategoryId });

        builder.Property(attribute => attribute.Name)
            .HasColumnName("name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(attribute => attribute.Required)
            .HasColumnName("required")
            .IsRequired();

        builder.Property(attribute => attribute.AllowCustom)
            .HasColumnName("allow_custom")
            .IsRequired();

        builder.Property(attribute => attribute.IsVariant)
            .HasColumnName("is_variant")
            .IsRequired();

        builder.Property(attribute => attribute.SyncedAt)
            .HasColumnName("synced_at")
            .IsRequired();
    }
}
