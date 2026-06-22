using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductVariantType = Catalog.Domain.Products.Variant;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Product aggregate kökünün EF Core eşleme yapılandırması.</summary>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.GroupId).HasColumnName("group_id").IsRequired();
        builder.Property(p => p.Name).HasColumnName("title").IsRequired().HasMaxLength(500);
        builder.Ignore(p => p.DomainEvents);

        builder.Property(p => p.ModelCode)
            .HasColumnName("product_sku")
            .HasConversion(code => code.Value, value => ModelCode.FromPersistence(value))
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(p => p.ModelCode).IsUnique();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<ProductStatus>(v, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.AttributeValues)
            .HasColumnName("attribute_values")
            .HasColumnType("jsonb")
            .HasConversion(
                values => ProductJsonPersistence.SerializeAttributeValues(values),
                json => ProductJsonPersistence.DeserializeAttributeValues(json))
            .IsRequired();

        builder.Property<List<ProductVariantType>>("_variants")
            .HasColumnName("variants")
            .HasColumnType("jsonb")
            .HasConversion(
                variants => ProductJsonPersistence.SerializeVariants(variants),
                json => ProductJsonPersistence.DeserializeVariants(json))
            .IsRequired();

        builder.Ignore(p => p.Variants);

        builder.Navigation(p => p.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_items");

        builder.HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey("ProductId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
