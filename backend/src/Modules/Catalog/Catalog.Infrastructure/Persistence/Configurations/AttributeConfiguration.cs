using Catalog.Domain.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainAttribute = Catalog.Domain.Attributes.Attribute;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Attribute varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class AttributeConfiguration : IEntityTypeConfiguration<DomainAttribute>
{
    public void Configure(EntityTypeBuilder<DomainAttribute> builder)
    {
        builder.ToTable("attributes");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(a => a.Name).HasColumnName("name").IsRequired().HasMaxLength(500);
        builder.Ignore(a => a.DomainEvents);

        var keyProperty = builder.Property(a => a.Key)
            .HasColumnName("key")
            .HasConversion(key => key.Value, value => AttributeKey.FromPersistence(value))
            .HasMaxLength(200)
            .IsRequired();

        keyProperty.Metadata.SetValueComparer(new ValueComparer<AttributeKey>(
            (left, right) => left!.Value == right!.Value,
            key => key.Value.GetHashCode(StringComparison.Ordinal),
            key => AttributeKey.FromPersistence(key.Value)));

        builder.HasIndex(a => a.Key).IsUnique();

        builder.Navigation(a => a.Values)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_values");

        builder.OwnsMany(a => a.Values, value =>
        {
            value.ToTable("attribute_values");
            value.WithOwner().HasForeignKey("AttributeId");
            value.Property<Guid>("AttributeId").HasColumnName("attribute_id");
            value.HasKey(v => v.Id);
            value.Property(v => v.Id).HasColumnName("id").ValueGeneratedNever();
            value.Property(v => v.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            value.Ignore(v => v.DomainEvents);
            value.HasIndex("AttributeId", nameof(AttributeValue.Name)).IsUnique();
        });
    }
}
