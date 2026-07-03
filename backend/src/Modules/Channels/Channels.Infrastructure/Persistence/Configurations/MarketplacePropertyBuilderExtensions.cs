using Channels.Domain.Marketplaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Channels.Infrastructure.Persistence.Configurations;

/// <summary>Marketplace value object EF dönüşüm yapılandırması.</summary>
internal static class MarketplacePropertyBuilderExtensions
{
    public static PropertyBuilder<Marketplace> ConfigureMarketplaceColumn(
        this PropertyBuilder<Marketplace> propertyBuilder,
        string columnName = "marketplace_code")
    {
        propertyBuilder
            .HasColumnName(columnName)
            .HasConversion(
                marketplace => marketplace.Code,
                code => Marketplace.FromPersistence(code))
            .HasMaxLength(Marketplace.MaxCodeLength)
            .IsRequired();

        propertyBuilder.Metadata.SetValueComparer(new ValueComparer<Marketplace>(
            (left, right) => left!.Code == right!.Code,
            marketplace => marketplace.Code.GetHashCode(StringComparison.Ordinal),
            marketplace => Marketplace.FromPersistence(marketplace.Code)));

        return propertyBuilder;
    }
}
