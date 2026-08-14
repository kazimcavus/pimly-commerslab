namespace Catalog.Application.Contracts;

/// <summary>Kategori-özellik ataması veri transfer nesnesi.</summary>
/// <remarks>Scope: "model" | "slicer" | "item".</remarks>
public sealed record CategoryAttributeDto(
    Guid CategoryAttributeId,
    Guid AttributeId,
    string Key,
    string Name,
    bool Required,
    int SortOrder,
    string Scope);
