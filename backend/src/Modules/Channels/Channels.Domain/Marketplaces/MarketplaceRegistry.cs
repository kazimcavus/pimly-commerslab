using SharedKernel;

namespace Channels.Domain.Marketplaces;

/// <summary>Platform pazaryeri tanımları — kod registry; tenant-scoped değil.</summary>
public static class MarketplaceRegistry
{
    private static readonly MarketplaceDefinition[] Definitions =
    [
        new(
            SupportedMarketplace.Trendyol,
            MarketplaceKey.FromPersistence("trendyol"),
            "Trendyol",
            IsActive: true),
    ];

    /// <summary>Aktif pazaryeri tanımlarını döndürür.</summary>
    public static IReadOnlyList<MarketplaceDefinition> ListActive() =>
        Definitions.Where(definition => definition.IsActive).ToList();

    /// <summary>Anahtar ile tanım arar.</summary>
    public static Result<MarketplaceDefinition> GetByKey(MarketplaceKey key)
    {
        var definition = Definitions.FirstOrDefault(definition => definition.Key == key);
        return definition is null
            ? Result.Failure<MarketplaceDefinition>(Error.NotFound("Marketplace not found."))
            : Result.Success(definition);
    }

    /// <summary>Ham anahtar string ile tanım arar.</summary>
    public static Result<MarketplaceDefinition> GetByKey(string key)
    {
        var keyResult = MarketplaceKey.Create(key);
        return keyResult.IsFailure
            ? Result.Failure<MarketplaceDefinition>(keyResult.Error)
            : GetByKey(keyResult.Value);
    }
}

/// <summary>Platform pazaryeri tanımı.</summary>
public sealed record MarketplaceDefinition(
    SupportedMarketplace Id,
    MarketplaceKey Key,
    string Name,
    bool IsActive);
