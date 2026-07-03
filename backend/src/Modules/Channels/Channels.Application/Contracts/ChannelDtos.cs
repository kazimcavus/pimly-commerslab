namespace Channels.Application.Contracts;

/// <summary>Pazaryeri API yanıt modeli.</summary>
public sealed record MarketplaceDto(
    string Key,
    string Name,
    bool IsActive,
    bool IsConfigured);

/// <summary>Pazaryeri bağlantı API yanıt modeli; gizli alanlar maskelenir.</summary>
public sealed record MarketplaceConnectionDto(
    Guid Id,
    string MarketplaceKey,
    string? SellerId,
    bool HasApiKey,
    bool HasApiSecret,
    string? ApiKeyHint,
    bool IsEnabled);
