namespace Catalog.Application.Categories.UpdateCategoryAttribute;

/// <summary>Kategori-özellik ataması güncelleme komutu.</summary>
public sealed record UpdateCategoryAttributeCommand(
    Guid Id,
    bool Required,
    int SortOrder);
