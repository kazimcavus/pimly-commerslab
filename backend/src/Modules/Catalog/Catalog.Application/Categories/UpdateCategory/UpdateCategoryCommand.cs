namespace Catalog.Application.Categories.UpdateCategory;

/// <summary>Mevcut kategoriyi güncelleme komutu.</summary>
public sealed record UpdateCategoryCommand(Guid Id, string Name, string? Code, Guid? ParentId);
