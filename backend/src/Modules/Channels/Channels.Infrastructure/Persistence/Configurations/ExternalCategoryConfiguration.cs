using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using Microsoft.EntityFrameworkCore;
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

        builder.Property(category => category.Marketplace)
            .ConfigureMarketplaceColumn();

        builder.Property(category => category.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(category => new { category.Marketplace, category.ExternalId }).IsUnique();
        builder.HasIndex(category => category.Marketplace);
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
