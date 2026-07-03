namespace Channels.Application.Contracts;

/// <summary>Catalog kategorisi ile pazaryeri harici kategorisi arasındaki eşleme DTO'su.</summary>
public sealed record CategoryChannelMappingDto(
    Guid Id,
    Guid CatalogCategoryId,
    string MarketplaceCode,
    string ExternalId,
    CatalogCategorySnapshotDto? CatalogCategory,
    ExternalCategorySummaryDto? ExternalCategory);

/// <summary>Catalog kategori özeti DTO'su.</summary>
public sealed record CatalogCategorySnapshotDto(
    Guid Id,
    string Name,
    string? Code);

/// <summary>Harici kategori özeti DTO'su.</summary>
public sealed record ExternalCategorySummaryDto(
    string ExternalId,
    string Name,
    string Path,
    bool IsLeaf,
    DateTimeOffset SyncedAt);

/// <summary>Ürün listeleme için çözümlenmiş eşleme DTO'su.</summary>
public sealed record ResolvedCategoryChannelMappingDto(
    string MarketplaceCode,
    Guid CatalogCategoryId,
    string ExternalId);
