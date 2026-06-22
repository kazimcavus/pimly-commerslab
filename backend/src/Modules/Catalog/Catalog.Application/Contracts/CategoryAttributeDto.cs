namespace Catalog.Application.Contracts;

/// <summary>Kategori-özellik ataması veri transfer nesnesi.</summary>
public sealed record CategoryAttributeDto(
    Guid CategoryAttributeId,
    Guid AttributeId,
    string Key,
    string Name,
    bool Required,
    bool MarketplaceRequired,
    int SortOrder);
