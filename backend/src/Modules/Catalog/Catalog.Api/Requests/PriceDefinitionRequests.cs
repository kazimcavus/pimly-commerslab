namespace Catalog.Api.Requests;

/// <summary>Yeni fiyat tanımı oluşturma isteğinin gövdesini temsil eder.</summary>
internal sealed record CreatePriceDefinitionRequest(string Name, string? Code);

/// <summary>Mevcut fiyat tanımı güncelleme isteğinin gövdesini temsil eder.</summary>
internal sealed record UpdatePriceDefinitionRequest(string Name, string? Code);
