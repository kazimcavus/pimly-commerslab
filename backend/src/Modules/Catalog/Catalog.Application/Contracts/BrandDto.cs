namespace Catalog.Application.Contracts;

/// <summary>Marka veri transfer nesnesi.</summary>
public sealed record BrandDto(Guid Id, string Name, string? Code);
