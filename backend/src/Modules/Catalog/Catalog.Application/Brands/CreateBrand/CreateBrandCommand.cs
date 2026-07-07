namespace Catalog.Application.Brands.CreateBrand;

/// <summary>Yeni marka oluşturma komutu.</summary>
public sealed record CreateBrandCommand(string Name, string? Code);
