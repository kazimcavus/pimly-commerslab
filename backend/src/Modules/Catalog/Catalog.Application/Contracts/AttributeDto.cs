namespace Catalog.Application.Contracts;

/// <summary>Özellik tanımı veri transfer nesnesi.</summary>
public sealed record AttributeDto(Guid Id, string Key, string Name);
