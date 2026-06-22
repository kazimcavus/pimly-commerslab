namespace Catalog.Application.Variants.AddVariantValue;

/// <summary>Varyant türüne yeni bir değer ekleme isteğini temsil eder.</summary>
public sealed record AddVariantValueCommand(
    Guid VariantTypeId,
    string Label,
    string? Color,
    string? ImageUrl,
    string? Code,
    int SortOrder);
