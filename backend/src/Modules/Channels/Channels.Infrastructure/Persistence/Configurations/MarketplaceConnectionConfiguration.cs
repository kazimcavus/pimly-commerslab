using Channels.Domain.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Channels.Infrastructure.Persistence.Configurations;

/// <summary>MarketplaceConnection aggregate kökünün EF Core eşleme yapılandırması.</summary>
internal sealed class MarketplaceConnectionConfiguration : IEntityTypeConfiguration<MarketplaceConnection>
{
    public void Configure(EntityTypeBuilder<MarketplaceConnection> builder)
    {
        builder.ToTable("marketplace_connections");

        builder.HasKey(connection => connection.Id);
        builder.Property(connection => connection.Id).HasColumnName("id");
        builder.Ignore(connection => connection.DomainEvents);

        builder.Property(connection => connection.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(connection => connection.Marketplace)
            .ConfigureMarketplaceColumn();

        builder.HasIndex(connection => new { connection.TenantId, connection.Marketplace }).IsUnique();

        builder.Property(connection => connection.SellerId)
            .HasColumnName("seller_id")
            .HasMaxLength(200);

        builder.Property(connection => connection.ApiKey)
            .HasColumnName("api_key")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(connection => connection.ApiSecret)
            .HasColumnName("api_secret")
            .HasMaxLength(500);

        builder.Property(connection => connection.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();
    }
}
