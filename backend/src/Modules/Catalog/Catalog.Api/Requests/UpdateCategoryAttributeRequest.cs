namespace Catalog.Api.Requests;

/// <summary>Kategori-öznitelik ataması güncelleme isteğinin gövdesini temsil eder.</summary>
/// <remarks>Scope: "model" | "slicer" | "item"; verilmezse mevcut seviye korunur.</remarks>
internal sealed record UpdateCategoryAttributeRequest(bool Required, int SortOrder, string? Scope = null);
