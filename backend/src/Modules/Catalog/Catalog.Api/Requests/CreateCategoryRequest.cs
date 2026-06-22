using System.Text.Json.Serialization;

namespace Catalog.Api.Requests;

/// <summary>Yeni kategori oluşturma isteğinin gövdesini temsil eder.</summary>
internal sealed record CreateCategoryRequest(
    string Name,
    string? Code,
    [property: JsonPropertyName("parent_id")] Guid? ParentId);
