using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.ChannelPrices;

namespace Pricing.Infrastructure.Persistence.Configurations;

/// <summary>ChannelPrice varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class ChannelPriceConfiguration : IEntityTypeConfiguration<ChannelPrice>
{
    public void Configure(EntityTypeBuilder<ChannelPrice> builder)
    {
        builder.ToTable("channel_prices");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.ProductItemId).HasColumnName("product_item_id").IsRequired();
        builder.Property(p => p.Marketplace).ConfigureMarketplaceColumn();
        builder.Property(p => p.Amount).HasColumnName("amount").HasColumnType("numeric(14,2)").IsRequired();
        builder.Property(p => p.CompareAtAmount).HasColumnName("compare_at_amount").HasColumnType("numeric(14,2)");
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Ignore(p => p.DomainEvents);

        // product_item_id, Catalog kalemine opak referanstır; bağlamlar arası FK kurulmaz.
        // Kalem × pazaryeri başına tek kanal fiyatı: benzersiz indeks.
        builder.HasIndex(p => new { p.ProductItemId, p.Marketplace }).IsUnique();
    }
}
