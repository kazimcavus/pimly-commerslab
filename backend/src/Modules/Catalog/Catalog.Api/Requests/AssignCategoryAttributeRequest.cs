namespace Catalog.Api.Requests;

/// <summary>Kategoriye öznitelik atama isteğinin gövdesini temsil eder.</summary>
internal sealed record AssignCategoryAttributeRequest(
    Guid AttributeId,
    bool Required,
    int SortOrder);
