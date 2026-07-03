namespace Channels.Application.Contracts;

public sealed record AttributeChannelMappingDto(
    Guid Id,
    Guid CatalogCategoryId,
    string MarketplaceKey,
    string SourceType,
    Guid CatalogSourceId,
    string ExternalAttributeId,
    CatalogAttributeSnapshotDto? CatalogAttribute,
    CatalogVariantSnapshotDto? CatalogVariant,
    ExternalCategoryAttributeSummaryDto? ExternalAttribute);

public sealed record CatalogAttributeSnapshotDto(Guid Id, string Key, string Name);

public sealed record CatalogVariantSnapshotDto(Guid Id, string Key, string Name);

public sealed record ExternalCategoryAttributeSummaryDto(
    string ExternalAttributeId,
    string Name,
    bool Required,
    bool AllowCustom,
    bool IsVariant);

public sealed record AttributeValueChannelMappingDto(
    Guid Id,
    Guid AttributeChannelMappingId,
    Guid CatalogValueId,
    string ExternalValueId,
    string? CatalogValueName,
    ExternalAttributeValueSummaryDto? ExternalValue);

public sealed record ExternalAttributeValueSummaryDto(
    string ExternalValueId,
    string Name);

public sealed record ResolvedAttributeChannelMappingDto(
    string MarketplaceKey,
    Guid CatalogCategoryId,
    string SourceType,
    Guid CatalogSourceId,
    string ExternalAttributeId);

public sealed record ResolvedAttributeValueChannelMappingDto(
    Guid AttributeChannelMappingId,
    Guid CatalogValueId,
    string ExternalValueId);
