namespace Channels.Application.Connections.UpsertMarketplaceConnection;

/// <summary>Pazaryeri bağlantısı oluşturma veya güncelleme komutu.</summary>
public sealed record UpsertMarketplaceConnectionCommand(
    string MarketplaceKey,
    string? SellerId,
    string ApiKey,
    string? ApiSecret,
    bool IsEnabled);
