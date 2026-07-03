using Channels.Domain.Marketplaces;

namespace Channels.Domain.Taxonomy;

/// <summary>ExternalCategory cache depo arabirimi.</summary>
public interface IExternalCategoryRepository
{
    Task<int> CountByMarketplaceAsync(MarketplaceKey marketplaceKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalCategory>> SearchAsync(
        MarketplaceKey marketplaceKey,
        string? query,
        int limit,
        CancellationToken cancellationToken = default);

    Task<ExternalCategory?> GetByExternalIdAsync(
        MarketplaceKey marketplaceKey,
        string externalId,
        CancellationToken cancellationToken = default);

    Task UpsertBatchAsync(
        MarketplaceKey marketplaceKey,
        IReadOnlyList<ExternalCategoryUpsert> categories,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken = default);
}

/// <summary>Harici kategori upsert girdisi.</summary>
public sealed record ExternalCategoryUpsert(
    string ExternalId,
    string Name,
    string? ParentExternalId,
    string Path,
    bool IsLeaf);
