using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>ProductImage varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property<Guid>("ProductId").HasColumnName("product_id").IsRequired();
        builder.Property(i => i.Url).HasColumnName("url").HasMaxLength(2000).IsRequired();
        builder.Property(i => i.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(i => i.AltText).HasColumnName("alt_text").HasMaxLength(500);
        builder.Property(i => i.IsPrimary).HasColumnName("is_primary").IsRequired();
        builder.Property(i => i.VariantValueId).HasColumnName("variant_value_id");
        builder.Ignore(i => i.DomainEvents);
    }
}
