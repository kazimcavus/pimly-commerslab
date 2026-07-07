using Catalog.Application.Products;
using Catalog.Domain.Products;

namespace Catalog.Application.Products.CreateProduct;

/// <summary>Yeni ürün oluşturma komutu.</summary>
public sealed record CreateProductCommand(
    Guid GroupId,
    Guid CategoryId,
    string ModelCode,
    string Name,
    string Status,
    IReadOnlyList<string>? CodeInputs,
    IReadOnlyList<AttributeValueInput>? AttributeValueInputs,
    IReadOnlyList<Variant>? Variants,
    IReadOnlyList<CreateProductItemInput> Items,
    Guid? BrandId = null,
    string? Description = null);

/// <summary>Yeni ürün kalemi oluşturma girdisi.</summary>
public sealed record CreateProductItemInput(
    string? Sku,
    string Barcode,
    string? Gtin,
    string? Mpn,
    Guid? AxisValueEntryId,
    string? AxisValue,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    IReadOnlyList<AttributeValueInput>? AttributeValueInputs,
    IReadOnlyList<VariantValueInput>? VariantValueInputs);
