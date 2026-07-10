using Inventory.Domain.StockLevels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

/// <summary>StockLevel varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.ToTable("stock_levels");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.ProductItemId).HasColumnName("product_item_id").IsRequired();
        builder.Property(s => s.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Ignore(s => s.DomainEvents);

        // product_item_id, Catalog kalemine opak referanstır; bağlamlar arası FK kurulmaz.
        // Kalem başına tek stok kaydı: benzersiz indeks.
        builder.HasIndex(s => s.ProductItemId).IsUnique();
    }
}
