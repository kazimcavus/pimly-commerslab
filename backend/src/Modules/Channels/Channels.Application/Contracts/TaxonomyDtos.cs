namespace Channels.Application.Contracts;

/// <summary>Taxonomy sync job API yanıt modeli.</summary>
public sealed record TaxonomySyncRunDto(
    Guid Id,
    string MarketplaceCode,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int ProcessedCount,
    int? TotalEstimate,
    string? ErrorMessage);

/// <summary>Taxonomy sync özet durumu.</summary>
public sealed record TaxonomyStatusDto(
    string MarketplaceCode,
    bool IsSyncActive,
    Guid? ActiveSyncRunId,
    DateTimeOffset? LastCompletedAt,
    int CachedCategoryCount,
    TaxonomySyncRunDto? LastCompletedRun);

/// <summary>Harici kategori arama sonucu.</summary>
public sealed record ExternalCategoryDto(
    Guid Id,
    string ExternalId,
    string Name,
    string? ParentExternalId,
    string Path,
    bool IsLeaf,
    DateTimeOffset SyncedAt);
