namespace Catalog.Application.Products.AddProductImage;

/// <summary>Ürün galerisine görsel ekleme komutu.</summary>
public sealed record AddProductImageCommand(
    Guid ProductId,
    string Url,
    int SortOrder,
    string? AltText,
    bool IsPrimary,
    Guid? VariantValueId);
