using Catalog.Application.Products;

namespace Catalog.Application.Products.UpdateProductItem;

/// <summary>Ürün kalemi güncelleme komutu.</summary>
public sealed record UpdateProductItemCommand(
    Guid Id,
    string? Gtin,
    string? Mpn,
    Guid? AxisValueEntryId,
    string? AxisValue,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    IReadOnlyList<AttributeValueInput>? AttributeValueInputs);
