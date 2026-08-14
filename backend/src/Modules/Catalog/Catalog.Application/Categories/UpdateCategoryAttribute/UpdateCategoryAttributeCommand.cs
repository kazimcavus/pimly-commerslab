using Catalog.Domain.Categories;

namespace Catalog.Application.Categories.UpdateCategoryAttribute;

/// <summary>Kategori-özellik ataması güncelleme komutu.</summary>
/// <remarks>Scope null verilirse mevcut seviye korunur (geriye uyumlu PATCH).</remarks>
public sealed record UpdateCategoryAttributeCommand(
    Guid Id,
    bool Required,
    int SortOrder,
    AttributeScope? Scope = null);
