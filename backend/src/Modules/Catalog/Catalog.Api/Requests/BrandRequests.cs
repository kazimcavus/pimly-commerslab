namespace Catalog.Api.Requests;

/// <summary>Yeni marka oluşturma isteğinin gövdesini temsil eder.</summary>
internal sealed record CreateBrandRequest(string Name, string? Code);

/// <summary>Mevcut marka güncelleme isteğinin gövdesini temsil eder.</summary>
internal sealed record UpdateBrandRequest(string Name, string? Code);
