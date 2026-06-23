namespace Catalog.Application.Variants.UpdateVariantValue;

/// <summary>Mevcut bir varyant değerini güncelleme isteğini temsil eder.</summary>
public sealed record UpdateVariantValueCommand(
    Guid Id,
    string Label,
    string? Color,
    string? ImageUrl,
    string? Key,
    int SortOrder);
