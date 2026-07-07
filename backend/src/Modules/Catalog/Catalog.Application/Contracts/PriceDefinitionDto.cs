namespace Catalog.Application.Contracts;

/// <summary>Fiyat tanımı veri transfer nesnesi.</summary>
public sealed record PriceDefinitionDto(Guid Id, string Name, string? Code);
