namespace Channels.Application.Connections;

/// <summary>Pazaryeri API çağrıları için kimlik bilgisi taşıyıcısı.</summary>
/// <remarks>Trendyol: Basic auth (ApiKey:ApiSecret) + istek yollarında SellerId kullanılır.</remarks>
public sealed record MarketplaceCredentials(
    string? SellerId,
    string ApiKey,
    string? ApiSecret);
