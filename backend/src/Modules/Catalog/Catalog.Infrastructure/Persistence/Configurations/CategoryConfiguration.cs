using Catalog.Domain.Attributes;
using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Category varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.Name).HasColumnName("name").IsRequired().HasMaxLength(500);
        builder.Property(c => c.Code).HasColumnName("code").HasMaxLength(100);
        builder.Property(c => c.ParentId).HasColumnName("parent_id");
        builder.Ignore(c => c.DomainEvents);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(c => c.Assignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_assignments");

        builder.OwnsMany(c => c.Assignments, assignment =>
        {
            assignment.ToTable("category_attributes");
            assignment.WithOwner().HasForeignKey("CategoryId");
            assignment.Property<Guid>("CategoryId").HasColumnName("category_id");
            assignment.HasKey(a => a.Id);
            assignment.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
            assignment.Property(a => a.AttributeId).HasColumnName("attribute_id").IsRequired();
            assignment.Property(a => a.Required).HasColumnName("required");
            assignment.Property(a => a.MarketplaceRequired).HasColumnName("marketplace_required");
            assignment.Property(a => a.SortOrder).HasColumnName("sort_order");
            assignment.Ignore(a => a.DomainEvents);
            assignment.HasIndex(a => new { a.AttributeId }).IsUnique(false);
            assignment.HasIndex("CategoryId", nameof(CategoryAttributeAssignment.AttributeId)).IsUnique();
        });
    }
}
