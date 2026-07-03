using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Channels.Infrastructure.Persistence.Configurations;

/// <summary>ExternalCategory varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class ExternalCategoryConfiguration : IEntityTypeConfiguration<ExternalCategory>
{
    public void Configure(EntityTypeBuilder<ExternalCategory> builder)
    {
        builder.ToTable("external_categories");

        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).HasColumnName("id");
        builder.Ignore(category => category.DomainEvents);

        var keyProperty = builder.Property(category => category.MarketplaceKey)
            .HasColumnName("marketplace_key")
            .HasConversion(key => key.Value, value => MarketplaceKey.FromPersistence(value))
            .HasMaxLength(MarketplaceKey.MaxLength)
            .IsRequired();

        keyProperty.Metadata.SetValueComparer(new ValueComparer<MarketplaceKey>(
            (left, right) => left!.Value == right!.Value,
            key => key.Value.GetHashCode(StringComparison.Ordinal),
            key => MarketplaceKey.FromPersistence(key.Value)));

        builder.Property(category => category.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(category => new { category.MarketplaceKey, category.ExternalId }).IsUnique();
        builder.HasIndex(category => category.MarketplaceKey);
        builder.HasIndex(category => category.Name);

        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(category => category.ParentExternalId)
            .HasColumnName("parent_external_id")
            .HasMaxLength(100);

        builder.Property(category => category.Path)
            .HasColumnName("path")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(category => category.IsLeaf)
            .HasColumnName("is_leaf")
            .IsRequired();

        builder.Property(category => category.SyncedAt)
            .HasColumnName("synced_at")
            .IsRequired();
    }
}
