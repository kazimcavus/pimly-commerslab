using Channels.Domain.Connections;
using Channels.Domain.Marketplaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        var keyProperty = builder.Property(connection => connection.MarketplaceKey)
            .HasColumnName("marketplace_key")
            .HasConversion(key => key.Value, value => MarketplaceKey.FromPersistence(value))
            .HasMaxLength(MarketplaceKey.MaxLength)
            .IsRequired();

        keyProperty.Metadata.SetValueComparer(new ValueComparer<MarketplaceKey>(
            (left, right) => left!.Value == right!.Value,
            key => key.Value.GetHashCode(StringComparison.Ordinal),
            key => MarketplaceKey.FromPersistence(key.Value)));

        builder.HasIndex(connection => new { connection.TenantId, connection.MarketplaceKey }).IsUnique();

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
