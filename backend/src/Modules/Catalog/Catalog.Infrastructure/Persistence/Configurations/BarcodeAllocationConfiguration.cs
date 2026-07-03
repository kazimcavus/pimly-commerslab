using Catalog.Domain.Barcodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Tenancy;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>BarcodeAllocation varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class BarcodeAllocationConfiguration : IEntityTypeConfiguration<BarcodeAllocation>
{
    public void Configure(EntityTypeBuilder<BarcodeAllocation> builder)
    {
        builder.ToTable("barcode_allocations");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(a => a.Barcode).HasColumnName("barcode").HasMaxLength(200).IsRequired();
        builder.Property(a => a.AllocatedAt).HasColumnName("allocated_at").IsRequired();
        builder.Ignore(a => a.DomainEvents);

        builder.HasIndex(TenantEntityShadowProperty.Name, nameof(BarcodeAllocation.Barcode)).IsUnique();
    }
}
