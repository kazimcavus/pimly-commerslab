namespace Catalog.Application.Products.UpdateProductImage;

/// <summary>Ürün galerisi görseli güncelleme komutu.</summary>
public sealed record UpdateProductImageCommand(
    Guid ImageId,
    string Url,
    int SortOrder,
    string? AltText,
    bool IsPrimary,
    Guid? VariantValueId);
