namespace Catalog.Application.Brands.UpdateBrand;

/// <summary>Mevcut markayı güncelleme komutu.</summary>
public sealed record UpdateBrandCommand(Guid Id, string Name, string? Code);
