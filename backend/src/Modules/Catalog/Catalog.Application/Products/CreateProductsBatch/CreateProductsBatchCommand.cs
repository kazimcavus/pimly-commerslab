using Catalog.Application.Products;
using Catalog.Domain.Products;

namespace Catalog.Application.Products.CreateProductsBatch;

/// <summary>Toplu ürün oluşturma komutu.</summary>
/// <remarks>
/// EnforceRequiredAttributes=false, pazaryeri import'u için zorunlu-özellik doğrulamasını atlar:
/// içeri alınan veri kaynağın gerçeğidir, PIM'de sonradan eklenen zorunluluklar importu bloklamaz.
/// </remarks>
public sealed record CreateProductsBatchCommand(
    Guid GroupId,
    IReadOnlyList<CreateProductsBatchItem> Products,
    bool EnforceRequiredAttributes = true);

/// <summary>Toplu oluşturma girdisindeki tek ürün tanımı.</summary>
/// <remarks>
/// SplitOverrides slicer değeri başına gerçek kod/ad taşır (pazaryeri import'u).
/// SplitAttributeValueInputs, slicer (renk) seviyeli özellik değerlerini bölünen ürüne bağlar.
/// </remarks>
public sealed record CreateProductsBatchItem(
    Guid CategoryId,
    string ModelCode,
    string Name,
    string Status,
    IReadOnlyList<string>? CodeInputs,
    IReadOnlyList<AttributeValueInput>? AttributeValueInputs,
    IReadOnlyList<Variant>? Variants,
    IReadOnlyList<CreateProduct.CreateProductItemInput> Items,
    IReadOnlyList<ProductSplitOverride>? SplitOverrides = null,
    Guid? BrandId = null,
    string? Description = null,
    IReadOnlyList<BatchSplitAttributeValues>? SplitAttributeValueInputs = null);

/// <summary>Tek slicer değerinin (ör. "Antrasit") ürününe yazılacak özellik değerleri.</summary>
public sealed record BatchSplitAttributeValues(
    string ValueName,
    IReadOnlyList<AttributeValueInput> Values);
