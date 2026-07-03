namespace Channels.Api.Requests;

/// <summary>Pazaryeri bağlantısı oluşturma veya güncelleme isteği gövdesi.</summary>
public sealed record UpsertMarketplaceConnectionRequest(
    string? SellerId,
    string ApiKey,
    string? ApiSecret,
    bool IsEnabled);
