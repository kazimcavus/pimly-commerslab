namespace Catalog.Api.Requests;

/// <summary>Mevcut varyant türü güncelleme isteğinin gövdesini temsil eder.</summary>
internal sealed record UpdateVariantTypeRequest(string Name, string? SelectionStyle, int SortOrder, bool Slicer);
