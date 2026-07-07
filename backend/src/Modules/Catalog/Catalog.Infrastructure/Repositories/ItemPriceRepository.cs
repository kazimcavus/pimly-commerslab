using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

/// <summary>ProductItemPrice aggregate için veritabanı erişim katmanı.</summary>
internal sealed class ItemPriceRepository(CatalogDbContext db) : IItemPriceRepository
{
    public async Task<ProductItemPrice?> GetAsync(
        Guid productItemId,
        Guid priceDefinitionId,
        CancellationToken cancellationToken = default) =>
        await db.ProductItemPrices
            .FirstOrDefaultAsync(
                p => p.ProductItemId == productItemId && p.PriceDefinitionId == priceDefinitionId,
                cancellationToken);

    public async Task<IReadOnlyList<ProductItemPrice>> ListByItemAsync(
        Guid productItemId,
        CancellationToken cancellationToken = default) =>
        await db.ProductItemPrices
            .Where(p => p.ProductItemId == productItemId)
            .OrderBy(p => p.PriceDefinitionId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ProductItemPrice itemPrice, CancellationToken cancellationToken = default) =>
        await db.ProductItemPrices.AddAsync(itemPrice, cancellationToken);

    public void Update(ProductItemPrice itemPrice) => db.ProductItemPrices.Update(itemPrice);

    public void Remove(ProductItemPrice itemPrice) => db.ProductItemPrices.Remove(itemPrice);
}
