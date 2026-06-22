using Catalog.Domain.Products;

namespace Catalog.Application.Contracts;

/// <summary>Ürün API yanıt modeli.</summary>
public sealed record ProductDto(
    Guid Id,
    Guid GroupId,
    string ModelCode,
    string Name,
    string Status,
    IReadOnlyList<ProductAttributeValueDto> AttributeValues,
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<ProductItemDto> Items);

/// <summary>Ürün kalemi API yanıt modeli.</summary>
public sealed record ProductItemDto(
    Guid Id,
    Guid ProductId,
    string? Sku,
    string Barcode,
    string? Gtin,
    string? Mpn,
    Guid? AxisValueEntryId,
    string? AxisValue,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    IReadOnlyList<ProductAttributeValueDto> AttributeValues,
    IReadOnlyList<ProductVariantValueDto> VariantValues);

/// <summary>Özellik değeri anlık görüntüsü API yanıt modeli.</summary>
public sealed record ProductAttributeValueDto(AttributeDto Attribute, Guid Id, string Name);

/// <summary>Üründe sabitlenen eksen tanım anlık görüntüsü API yanıt modeli.</summary>
public sealed record ProductVariantDto(
    Guid Id,
    string Name,
    string SelectionStyle,
    bool Slicer = false);

/// <summary>Eksen değeri anlık görüntüsü API yanıt modeli.</summary>
public sealed record ProductVariantValueDto(ProductVariantDto Variant, Guid Id, string Name);
