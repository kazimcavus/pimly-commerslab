using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Tenancy;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>ProductItem varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class ProductItemConfiguration : IEntityTypeConfiguration<ProductItem>
{
    public void Configure(EntityTypeBuilder<ProductItem> builder)
    {
        builder.ToTable("product_items");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id");
        builder.Property<Guid>("ProductId").HasColumnName("product_id").IsRequired();
        builder.Property(v => v.Sku).HasColumnName("sku").HasMaxLength(200);
        builder.Property(v => v.Barcode).HasColumnName("barcode").HasMaxLength(200).IsRequired();
        builder.Property(v => v.Gtin).HasColumnName("gtin").HasMaxLength(50);
        builder.Property(v => v.Mpn).HasColumnName("mpn").HasMaxLength(100);
        builder.Property(v => v.AxisValueEntryId).HasColumnName("axis_value_entry_id");
        builder.Property(v => v.AxisValue).HasColumnName("axis_value").HasMaxLength(500);
        builder.Ignore(v => v.DomainEvents);

        builder.HasIndex(TenantEntityShadowProperty.Name, nameof(ProductItem.Barcode)).IsUnique();
        builder.HasIndex(TenantEntityShadowProperty.Name, nameof(ProductItem.Sku))
            .IsUnique()
            .HasFilter("sku IS NOT NULL");

        builder.Property(v => v.AttributeValues)
            .HasColumnName("attribute_values")
            .HasColumnType("jsonb")
            .HasConversion(
                values => ProductJsonPersistence.SerializeAttributeValues(values),
                json => ProductJsonPersistence.DeserializeAttributeValues(json))
            .IsRequired();

        builder.Property(v => v.VariantValues)
            .HasColumnName("variant_values")
            .HasColumnType("jsonb")
            .HasConversion(
                values => ProductJsonPersistence.SerializeVariantValues(values),
                json => ProductJsonPersistence.DeserializeVariantValues(json))
            .IsRequired();
    }
}
