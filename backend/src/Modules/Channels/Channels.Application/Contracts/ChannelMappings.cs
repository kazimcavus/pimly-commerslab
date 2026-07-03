using Channels.Application.Contracts;
using Channels.Domain.Connections;
using Channels.Domain.Marketplaces;

namespace Channels.Application.Contracts;

/// <summary>Channels domain modelleri ile DTO'lar arasında dönüşüm sağlar.</summary>
internal static class ChannelMappings
{
    internal static MarketplaceDto ToDto(this MarketplaceDefinition marketplace, bool isConfigured) =>
        new(
            marketplace.Key.Value,
            marketplace.Name,
            marketplace.IsActive,
            isConfigured);

    internal static MarketplaceConnectionDto ToDto(this MarketplaceConnection connection) =>
        new(
            connection.Id,
            connection.MarketplaceKey.Value,
            connection.SellerId,
            !string.IsNullOrWhiteSpace(connection.ApiKey),
            !string.IsNullOrWhiteSpace(connection.ApiSecret),
            CreateHint(connection.ApiKey),
            connection.IsEnabled);

    private static string? CreateHint(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        return apiKey.Length <= 4 ? apiKey : apiKey[^4..];
    }
}
