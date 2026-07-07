using Catalog.Application.Products;

namespace Catalog.Application.Products.UpdateProduct;

/// <summary>Ürün güncelleme komutu.</summary>
public sealed record UpdateProductCommand(
    Guid Id,
    Guid CategoryId,
    string Name,
    string Status,
    IReadOnlyList<AttributeValueInput>? AttributeValueInputs,
    Guid? BrandId = null,
    string? Description = null);
