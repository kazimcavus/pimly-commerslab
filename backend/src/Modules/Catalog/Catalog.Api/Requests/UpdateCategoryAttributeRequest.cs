namespace Catalog.Api.Requests;

/// <summary>Kategori-öznitelik eşlemesi güncelleme isteğinin gövdesini temsil eder.</summary>
internal sealed record UpdateCategoryAttributeRequest(bool Required, bool MarketplaceRequired, int SortOrder);
