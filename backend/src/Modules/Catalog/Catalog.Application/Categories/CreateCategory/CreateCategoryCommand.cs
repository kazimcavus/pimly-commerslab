namespace Catalog.Application.Categories.CreateCategory;

/// <summary>Yeni kategori oluşturma komutu.</summary>
public sealed record CreateCategoryCommand(string Name, string? Code, Guid? ParentId);
