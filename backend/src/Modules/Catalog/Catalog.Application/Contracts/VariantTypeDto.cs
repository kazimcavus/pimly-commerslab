namespace Catalog.Application.Contracts;

/// <summary>Varyant tipi veri transfer nesnesi.</summary>
public sealed record VariantTypeDto(
    Guid Id,
    string Name,
    string SelectionStyle,
    int SortOrder,
    bool Slicer);
