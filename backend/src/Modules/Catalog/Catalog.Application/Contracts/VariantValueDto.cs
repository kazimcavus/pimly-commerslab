namespace Catalog.Application.Contracts;

/// <summary>Varyant değeri veri transfer nesnesi.</summary>
public sealed record VariantValueDto(
    Guid Id,
    Guid VariantTypeId,
    string Label,
    string? Color,
    string? ImageUrl,
    string? Code,
    int SortOrder);
