namespace Channels.Application.Contracts;

/// <summary>Harici kategori attribute DTO'su.</summary>
public sealed record ExternalCategoryAttributeDto(
    string ExternalCategoryId,
    string ExternalAttributeId,
    string Name,
    bool Required,
    bool AllowCustom,
    bool IsVariant,
    DateTimeOffset SyncedAt,
    IReadOnlyList<ExternalAttributeValueDto> Values,
    bool IsSlicer = false);

/// <summary>Harici attribute değer DTO'su.</summary>
public sealed record ExternalAttributeValueDto(
    string ExternalAttributeId,
    string ExternalValueId,
    string Name,
    DateTimeOffset SyncedAt);
