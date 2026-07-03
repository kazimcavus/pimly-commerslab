using Catalog.Application.Products;
using Catalog.Domain.Products;

namespace Catalog.Application.Products.CreateProductsBatch;

/// <summary>Toplu ürün oluşturma komutu.</summary>
public sealed record CreateProductsBatchCommand(
    Guid GroupId,
    IReadOnlyList<CreateProductsBatchItem> Products);

/// <summary>Toplu oluşturma girdisindeki tek ürün tanımı.</summary>
public sealed record CreateProductsBatchItem(
    Guid CategoryId,
    string ModelCode,
    string Name,
    string Status,
    IReadOnlyList<string>? CodeInputs,
    IReadOnlyList<AttributeValueInput>? AttributeValueInputs,
    IReadOnlyList<Variant>? Variants,
    IReadOnlyList<CreateProduct.CreateProductItemInput> Items);
