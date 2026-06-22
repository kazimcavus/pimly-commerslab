using Catalog.Domain;
using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

/// <summary>Product aggregate için veritabanı erişim katmanı.</summary>
internal sealed class ProductRepository(CatalogDbContext db) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Products
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Product?> GetByModelCodeAsync(string modelCode, CancellationToken cancellationToken = default)
    {
        var code = ModelCode.FromPersistence(modelCode.Trim());
        return await db.Products
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.ModelCode == code, cancellationToken);
    }

    public async Task<ProductItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        await db.ProductItems.FirstOrDefaultAsync(v => v.Id == itemId, cancellationToken);

    public async Task<Product?> GetByItemIdAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var productId = await db.ProductItems
            .Where(v => v.Id == itemId)
            .Select(v => EF.Property<Guid>(v, "ProductId"))
            .FirstOrDefaultAsync(cancellationToken);

        if (productId == Guid.Empty)
        {
            return null;
        }

        return await GetByIdAsync(productId, cancellationToken);
    }

    public async Task<bool> ModelCodeExistsAsync(string modelCode, CancellationToken cancellationToken = default)
    {
        var code = ModelCode.FromPersistence(modelCode.Trim());
        return await db.Products.AnyAsync(p => p.ModelCode == code, cancellationToken);
    }

    public async Task<bool> BarcodeExistsAsync(string barcode, CancellationToken cancellationToken = default) =>
        await db.ProductItems.AnyAsync(v => v.Barcode == barcode, cancellationToken);

    public async Task<bool> VariantSkuExistsAsync(string sku, CancellationToken cancellationToken = default) =>
        await db.ProductItems.AnyAsync(v => v.Sku == sku, cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        await db.Products.AddAsync(product, cancellationToken);

    public void Update(Product product) => db.Products.Update(product);

    public void Remove(Product product) => db.Products.Remove(product);
}
