using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.ItemPrices;
using Pricing.Domain.PriceDefinitions;

namespace Pricing.Infrastructure.Persistence.Configurations;

/// <summary>ProductItemPrice varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class ProductItemPriceConfiguration : IEntityTypeConfiguration<ProductItemPrice>
{
    public void Configure(EntityTypeBuilder<ProductItemPrice> builder)
    {
        builder.ToTable("product_item_prices");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.ProductItemId).HasColumnName("product_item_id").IsRequired();
        builder.Property(p => p.PriceDefinitionId).HasColumnName("price_definition_id").IsRequired();
        builder.Property(p => p.Amount).HasColumnName("amount").HasColumnType("numeric(14,2)").IsRequired();
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Ignore(p => p.DomainEvents);

        // product_item_id, Catalog bağlamındaki kaleme opak referanstır; bağlamlar arası
        // yabancı anahtar kurulmaz. Kalem yaşam döngüsü ProductItemDeleted olayıyla senkronlanır.
        builder.HasOne<PriceDefinition>()
            .WithMany()
            .HasForeignKey(p => p.PriceDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.ProductItemId, p.PriceDefinitionId }).IsUnique();
    }
}
