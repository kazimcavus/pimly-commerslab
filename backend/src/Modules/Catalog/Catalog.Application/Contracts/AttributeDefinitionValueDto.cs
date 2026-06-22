namespace Catalog.Application.Contracts;

/// <summary>Özellik tanımı değeri veri transfer nesnesi.</summary>
public sealed record AttributeDefinitionValueDto(Guid Id, Guid AttributeId, string Name);
