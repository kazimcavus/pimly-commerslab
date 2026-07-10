namespace Inventory.Domain.StockLevels;

/// <summary>Kalem stok kayıtları için veritabanı erişim sözleşmesi.</summary>
public interface IStockLevelRepository
{
    /// <summary>Kaleme ait stok kaydını getirir; yoksa null.</summary>
    Task<StockLevel?> GetByItemAsync(Guid productItemId, CancellationToken cancellationToken = default);

    /// <summary>Yeni stok kaydı ekler.</summary>
    Task AddAsync(StockLevel stockLevel, CancellationToken cancellationToken = default);

    /// <summary>Stok kaydını günceller.</summary>
    void Update(StockLevel stockLevel);

    /// <summary>Stok kaydını siler.</summary>
    void Remove(StockLevel stockLevel);
}
