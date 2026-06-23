using Catalog.Domain.Barcodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>BarcodeSequence varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class BarcodeSequenceConfiguration : IEntityTypeConfiguration<BarcodeSequence>
{
    public void Configure(EntityTypeBuilder<BarcodeSequence> builder)
    {
        builder.ToTable("barcode_sequence");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.NextValue).HasColumnName("next_value").IsRequired();
        builder.Property(s => s.ClientAllocationRequired)
            .HasColumnName("client_allocation_required")
            .IsRequired()
            .HasDefaultValue(false);
        builder.Ignore(s => s.DomainEvents);
    }
}
