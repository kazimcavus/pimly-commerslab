using Inventory.Domain.StockLevels;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

/// <summary>StockLevel aggregate için veritabanı erişim katmanı.</summary>
internal sealed class StockLevelRepository(InventoryDbContext db) : IStockLevelRepository
{
    public async Task<StockLevel?> GetByItemAsync(Guid productItemId, CancellationToken cancellationToken = default) =>
        await db.StockLevels.FirstOrDefaultAsync(s => s.ProductItemId == productItemId, cancellationToken);

    public async Task AddAsync(StockLevel stockLevel, CancellationToken cancellationToken = default) =>
        await db.StockLevels.AddAsync(stockLevel, cancellationToken);

    public void Update(StockLevel stockLevel) => db.StockLevels.Update(stockLevel);

    public void Remove(StockLevel stockLevel) => db.StockLevels.Remove(stockLevel);
}
