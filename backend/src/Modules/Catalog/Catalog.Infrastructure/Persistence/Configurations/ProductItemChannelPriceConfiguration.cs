using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Tenancy;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>ProductItemChannelPrice varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class ProductItemChannelPriceConfiguration : IEntityTypeConfiguration<ProductItemChannelPrice>
{
    public void Configure(EntityTypeBuilder<ProductItemChannelPrice> builder)
    {
        builder.ToTable("product_item_channel_prices");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.ProductItemId).HasColumnName("product_item_id").IsRequired();
        builder.Property(p => p.MarketplaceKey)
            .HasColumnName("marketplace_key")
            .HasMaxLength(ProductItemChannelPrice.MarketplaceKeyMaxLength)
            .IsRequired();
        builder.Property(p => p.Price).HasColumnName("price").HasColumnType("numeric(14,2)").IsRequired();
        builder.Property(p => p.CompareAtPrice).HasColumnName("compare_at_price").HasColumnType("numeric(14,2)");
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Ignore(p => p.DomainEvents);

        builder.HasOne<ProductItem>()
            .WithMany()
            .HasForeignKey(p => p.ProductItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.ProductItemId, p.MarketplaceKey }).IsUnique();
        builder.HasIndex(TenantEntityShadowProperty.Name, nameof(ProductItemChannelPrice.MarketplaceKey));
    }
}
