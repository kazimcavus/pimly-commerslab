using Identity.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>Tenant aggregate kökünün EF Core eşleme yapılandırması.</summary>
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Id).HasColumnName("id");
        builder.Ignore(tenant => tenant.DomainEvents);

        builder.Property(tenant => tenant.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(tenant => tenant.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
