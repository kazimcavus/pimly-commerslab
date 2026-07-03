using Identity.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>TenantMembership varlığının EF Core eşleme yapılandırması.</summary>
internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("tenant_memberships");

        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.Id).HasColumnName("id");
        builder.Ignore(membership => membership.DomainEvents);

        builder.Property(membership => membership.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(membership => membership.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(membership => membership.IsPrimary)
            .HasColumnName("is_primary")
            .IsRequired();

        builder.Property(membership => membership.JoinedAt)
            .HasColumnName("joined_at")
            .IsRequired();

        builder.HasIndex(membership => membership.UserId);
        builder.HasIndex(membership => new { membership.TenantId, membership.UserId }).IsUnique();
        builder.HasIndex(membership => new { membership.UserId, membership.IsPrimary });
    }
}
