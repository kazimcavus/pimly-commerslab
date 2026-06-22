namespace Catalog.Application.Categories.UpdateCategoryAttribute;

/// <summary>Kategori-özellik atamasını güncelleme komutu.</summary>
public sealed record UpdateCategoryAttributeCommand(
    Guid Id,
    bool Required,
    bool MarketplaceRequired,
    int SortOrder);
