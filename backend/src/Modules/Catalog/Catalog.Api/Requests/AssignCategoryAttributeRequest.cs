namespace Catalog.Api.Requests;

/// <summary>Kategoriye öznitelik atama isteğinin gövdesini temsil eder.</summary>
/// <remarks>Scope: "model" | "slicer" | "item"; verilmezse "model".</remarks>
internal sealed record AssignCategoryAttributeRequest(
    Guid AttributeId,
    bool Required,
    int SortOrder,
    string? Scope = null);
