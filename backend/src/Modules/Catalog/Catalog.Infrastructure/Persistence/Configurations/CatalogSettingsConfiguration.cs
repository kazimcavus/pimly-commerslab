using Catalog.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Tenancy;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>CatalogSettings varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class CatalogSettingsConfiguration : IEntityTypeConfiguration<CatalogSettings>
{
    public void Configure(EntityTypeBuilder<CatalogSettings> builder)
    {
        builder.ToTable("catalog_settings");

        builder.HasKey(TenantEntityShadowProperty.Name, nameof(CatalogSettings.Id));
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.SlicerNamePosition)
            .HasColumnName("slicer_name_position")
            .IsRequired()
            .HasMaxLength(10);
        builder.Ignore(s => s.DomainEvents);
    }
}
