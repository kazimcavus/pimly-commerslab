namespace Catalog.Application.PriceDefinitions.CreatePriceDefinition;

/// <summary>Yeni fiyat tanımı oluşturma komutu.</summary>
public sealed record CreatePriceDefinitionCommand(string Name, string? Code);
