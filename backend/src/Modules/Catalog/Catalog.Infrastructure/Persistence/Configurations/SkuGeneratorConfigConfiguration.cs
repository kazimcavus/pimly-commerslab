using Catalog.Domain.SkuGenerator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

/// <summary>SkuGeneratorConfig varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class SkuGeneratorConfigConfiguration : IEntityTypeConfiguration<SkuGeneratorConfig>
{
    public void Configure(EntityTypeBuilder<SkuGeneratorConfig> builder)
    {
        builder.ToTable("sku_generator_config");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.Enabled).HasColumnName("enabled").IsRequired();
        builder.Property(c => c.CounterNextValue).HasColumnName("counter_next_value").IsRequired();

        builder.Property<List<SkuSegment>>("_segments")
            .HasColumnName("segments")
            .HasColumnType("jsonb")
            .HasConversion(
                segments => SkuGeneratorJsonPersistence.SerializeSegments(segments),
                json => SkuGeneratorJsonPersistence.DeserializeSegments(json))
            .IsRequired();

        builder.Ignore(c => c.Segments);
        builder.Ignore(c => c.DomainEvents);
    }
}
