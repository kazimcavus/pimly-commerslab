namespace Catalog.Application.Contracts;

/// <summary>Kategori veri transfer nesnesi.</summary>
public sealed record CategoryDto(Guid Id, string Name, string? Code, Guid? ParentId);
