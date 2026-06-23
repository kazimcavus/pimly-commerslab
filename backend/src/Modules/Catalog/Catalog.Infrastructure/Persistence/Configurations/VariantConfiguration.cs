using Catalog.Domain.Variants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Variant varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class VariantConfiguration : IEntityTypeConfiguration<Variant>
{
    public void Configure(EntityTypeBuilder<Variant> builder)
    {
        builder.ToTable("variants");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(v => v.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(v => v.SortOrder).HasColumnName("sort_order");
        builder.Property(v => v.Slicer).HasColumnName("slicer").HasDefaultValue(false);
        builder.Ignore(v => v.DomainEvents);

        var keyProperty = builder.Property(v => v.Key)
            .HasColumnName("key")
            .HasConversion(key => key.Value, value => VariantKey.FromPersistence(value))
            .HasMaxLength(200)
            .IsRequired();

        keyProperty.Metadata.SetValueComparer(new ValueComparer<VariantKey>(
            (left, right) => left!.Value == right!.Value,
            key => key.Value.GetHashCode(StringComparison.Ordinal),
            key => VariantKey.FromPersistence(key.Value)));

        builder.HasIndex(v => v.Key).IsUnique();
        builder.HasIndex(v => v.Name).IsUnique();
        builder.HasIndex(v => v.Slicer)
            .IsUnique()
            .HasFilter("slicer = true");

        builder.Property(v => v.SelectionStyle)
            .HasColumnName("selection_style")
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<SelectionStyle>(v, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Navigation(v => v.Values)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_values");

        builder.OwnsMany(v => v.Values, value =>
        {
            value.ToTable("variant_values");
            value.WithOwner().HasForeignKey("VariantId");
            value.Property<Guid>("VariantId").HasColumnName("variant_id");
            value.HasKey(vv => vv.Id);
            value.Property(vv => vv.Id).HasColumnName("id").ValueGeneratedNever();
            value.Property(vv => vv.Label).HasColumnName("label").IsRequired().HasMaxLength(200);
            value.Property(vv => vv.Color).HasColumnName("color").HasMaxLength(50);
            value.Property(vv => vv.ImageUrl).HasColumnName("image_url").HasMaxLength(2000);
            value.Property(vv => vv.Code).HasColumnName("code").HasMaxLength(100);
            value.Property(vv => vv.SortOrder).HasColumnName("sort_order");
            value.Ignore(vv => vv.DomainEvents);
            value.HasIndex("VariantId", nameof(VariantValue.Label)).IsUnique();
        });
    }
}
