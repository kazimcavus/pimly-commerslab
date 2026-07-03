namespace Catalog.Application.Categories.AssignCategoryAttribute;

/// <summary>Kategoriye özellik atama komutu.</summary>
public sealed record AssignCategoryAttributeCommand(
    Guid CategoryId,
    Guid AttributeId,
    bool Required,
    int SortOrder);
