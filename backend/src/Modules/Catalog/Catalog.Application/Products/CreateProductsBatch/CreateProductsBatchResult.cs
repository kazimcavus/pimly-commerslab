using Catalog.Application.Contracts;

namespace Catalog.Application.Products.CreateProductsBatch;

/// <summary>Toplu ürün oluşturma işleminin sonucu.</summary>
public sealed record CreateProductsBatchResult(IReadOnlyList<ProductDto> Products);
