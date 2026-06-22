using Catalog.Domain.Products;
using SharedKernel;

namespace Catalog.Domain;

/// <summary>Product aggregate kalıcılık işlemlerini tanımlayan depo arabirimi.</summary>
/// <example>ModelCodeExistsAsync("GOMlek-001") ile benzersizlik kontrolü.</example>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<Product>> ListAsync(Pagination pagination, CancellationToken cancellationToken = default);

    Task<Product?> GetByModelCodeAsync(string modelCode, CancellationToken cancellationToken = default);

    Task<ProductItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<Product?> GetByItemIdAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<bool> ModelCodeExistsAsync(string modelCode, CancellationToken cancellationToken = default);

    Task<bool> BarcodeExistsAsync(string barcode, CancellationToken cancellationToken = default);

    Task<bool> VariantSkuExistsAsync(string sku, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    void Update(Product product);

    void Remove(Product product);
}
