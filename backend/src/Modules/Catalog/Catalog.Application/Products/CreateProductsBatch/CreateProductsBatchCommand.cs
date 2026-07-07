using Catalog.Application.Products;
using Catalog.Domain.Products;

namespace Catalog.Application.Products.CreateProductsBatch;

/// <summary>Toplu ürün oluşturma komutu.</summary>
public sealed record CreateProductsBatchCommand(
    Guid GroupId,
    IReadOnlyList<CreateProductsBatchItem> Products);

/// <summary>Toplu oluşturma girdisindeki tek ürün tanımı.</summary>
/// <remarks>SplitOverrides slicer değeri başına gerçek kod/ad taşır (pazaryeri import'u).</remarks>
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
    string? Description = null);
