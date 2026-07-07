namespace Catalog.Application.PriceDefinitions.UpdatePriceDefinition;

/// <summary>Mevcut fiyat tanımını güncelleme komutu.</summary>
public sealed record UpdatePriceDefinitionCommand(Guid Id, string Name, string? Code);
