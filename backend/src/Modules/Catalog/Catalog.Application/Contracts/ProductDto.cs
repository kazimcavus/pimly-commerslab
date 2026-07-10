using Catalog.Domain.Products;

namespace Catalog.Application.Contracts;

/// <summary>Ürün API yanıt modeli.</summary>
/// <remarks>GroupCode grubun paylaşılan kodu (pazaryeri "model kodu"), SlicerValue bölünen eksen değeridir (ör. renk).</remarks>
public sealed record ProductDto(
    Guid Id,
    Guid GroupId,
    Guid CategoryId,
    string ModelCode,
    string Name,
    string Status,
    IReadOnlyList<ProductAttributeValueDto> AttributeValues,
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<ProductItemDto> Items,
    IReadOnlyList<ProductImageDto> Images,
    string? GroupCode = null,
    string? SlicerValue = null,
    Guid? BrandId = null,
    string? BrandName = null,
    string? Description = null);

/// <summary>Ürün galerisi görseli API yanıt modeli.</summary>
public sealed record ProductImageDto(
    Guid Id,
    string Url,
    int SortOrder,
    string? AltText,
    bool IsPrimary,
    Guid? VariantValueId);

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
