using Catalog.Application.Products;

namespace Catalog.Application.Products.UpdateProductItem;

/// <summary>Ürün kalemi güncelleme komutu.</summary>
/// <remarks>Sku/Barcode null ise mevcut değer korunur; boş Sku metni SKU'yu temizler.</remarks>
public sealed record UpdateProductItemCommand(
    Guid Id,
    string? Gtin,
    string? Mpn,
    Guid? AxisValueEntryId,
    string? AxisValue,
    IReadOnlyList<AttributeValueInput>? AttributeValueInputs,
    string? Sku = null,
    string? Barcode = null);
