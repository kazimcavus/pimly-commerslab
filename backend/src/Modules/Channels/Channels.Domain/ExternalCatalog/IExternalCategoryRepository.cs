using Channels.Domain.Marketplaces;

namespace Channels.Domain.ExternalCatalog;

/// <summary>ExternalCategory cache depo arabirimi.</summary>
public interface IExternalCategoryRepository
{
    Task<int> CountByMarketplaceAsync(Marketplace marketplace, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalCategory>> SearchAsync(
        Marketplace marketplace,
        string? query,
        int limit,
        CancellationToken cancellationToken = default);

    Task<ExternalCategory?> GetByExternalIdAsync(
        Marketplace marketplace,
        string externalId,
        CancellationToken cancellationToken = default);

    Task UpsertBatchAsync(
        Marketplace marketplace,
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
