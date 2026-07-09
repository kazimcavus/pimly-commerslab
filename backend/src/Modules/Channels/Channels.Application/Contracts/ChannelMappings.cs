using Channels.Application.Contracts;
using Channels.Domain.Connections;
using SharedKernel;

namespace Channels.Application.Contracts;

/// <summary>Channels domain modelleri ile DTO'lar arasında dönüşüm sağlar.</summary>
internal static class ChannelMappings
{
    internal static MarketplaceDto ToDto(this Marketplace marketplace, bool isConfigured) =>
        new(
            marketplace.Code,
            marketplace.Name,
            IsActive: true,
            isConfigured);

    internal static MarketplaceConnectionDto ToDto(this MarketplaceConnection connection) =>
        new(
            connection.Id,
            connection.Marketplace.Code,
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
