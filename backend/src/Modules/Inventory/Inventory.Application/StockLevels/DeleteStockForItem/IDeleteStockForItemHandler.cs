using SharedKernel;

namespace Inventory.Application.StockLevels.DeleteStockForItem;

/// <summary>Kaleme ait stok kaydını silme işlemini tanımlayan handler arabirimi.</summary>
public interface IDeleteStockForItemHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(
        DeleteStockForItemCommand command,
        CancellationToken cancellationToken = default);
}
