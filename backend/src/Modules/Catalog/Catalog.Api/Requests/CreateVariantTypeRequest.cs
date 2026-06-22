namespace Catalog.Api.Requests;

/// <summary>Yeni varyant türü oluşturma isteğinin gövdesini temsil eder.</summary>
internal sealed record CreateVariantTypeRequest(string Name, string? SelectionStyle, int SortOrder, bool Slicer = false);
