namespace Catalog.Api.Requests;

/// <summary>Varyant değeri oluşturma veya güncelleme isteğinin gövdesini temsil eder.</summary>
internal sealed record VariantValueRequest(
    string Label,
    string? Color,
    string? ImageUrl,
    string? Code,
    int SortOrder);
