namespace Catalog.Api.Requests;

/// <summary>Kategori-öznitelik ataması güncelleme isteğinin gövdesini temsil eder.</summary>
internal sealed record UpdateCategoryAttributeRequest(bool Required, int SortOrder);
