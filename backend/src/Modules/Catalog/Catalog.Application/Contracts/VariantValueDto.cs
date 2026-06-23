namespace Catalog.Application.Contracts;

/// <summary>Varyant değeri veri transfer nesnesi.</summary>
public sealed record VariantValueDto(
    Guid Id,
    Guid VariantTypeId,
    string Key,
    string Label,
    string? Color,
    string? ImageUrl,
    int SortOrder);
