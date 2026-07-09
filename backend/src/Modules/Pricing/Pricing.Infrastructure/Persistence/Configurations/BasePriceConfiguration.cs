using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.BasePrices;

namespace Pricing.Infrastructure.Persistence.Configurations;

/// <summary>BasePrice varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class BasePriceConfiguration : IEntityTypeConfiguration<BasePrice>
{
    public void Configure(EntityTypeBuilder<BasePrice> builder)
    {
        builder.ToTable("base_prices");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.ProductItemId).HasColumnName("product_item_id").IsRequired();
        builder.Property(p => p.Amount).HasColumnName("amount").HasColumnType("numeric(14,2)").IsRequired();
        builder.Property(p => p.CompareAtAmount).HasColumnName("compare_at_amount").HasColumnType("numeric(14,2)");
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Ignore(p => p.DomainEvents);

        // product_item_id, Catalog bağlamındaki kaleme opak referanstır; bağlamlar arası yabancı
        // anahtar kurulmaz. Kalem başına tek temel fiyat: benzersiz indeks.
        builder.HasIndex(p => p.ProductItemId).IsUnique();
    }
}
