using Catalog.Domain.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Tenancy;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Brand varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(b => b.Name).HasColumnName("name").IsRequired().HasMaxLength(500);
        builder.Property(b => b.Code).HasColumnName("code").HasMaxLength(100);
        builder.Ignore(b => b.DomainEvents);

        // tenant_id shadow property tenancy uzantısı tarafından eklenir; burada eşlenmez.
        builder.HasIndex(TenantEntityShadowProperty.Name, nameof(Brand.Name)).IsUnique();
    }
}
