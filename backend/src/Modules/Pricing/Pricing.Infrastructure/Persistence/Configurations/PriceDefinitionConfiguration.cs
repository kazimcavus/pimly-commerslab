using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.PriceDefinitions;
using SharedKernel.Tenancy;

namespace Pricing.Infrastructure.Persistence.Configurations;

/// <summary>PriceDefinition varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class PriceDefinitionConfiguration : IEntityTypeConfiguration<PriceDefinition>
{
    public void Configure(EntityTypeBuilder<PriceDefinition> builder)
    {
        builder.ToTable("price_definitions");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(d => d.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(d => d.Code).HasColumnName("code").HasMaxLength(100);
        builder.Ignore(d => d.DomainEvents);

        // tenant_id shadow property tenancy uzantısı tarafından eklenir; burada eşlenmez.
        builder.HasIndex(TenantEntityShadowProperty.Name, nameof(PriceDefinition.Name)).IsUnique();
    }
}
