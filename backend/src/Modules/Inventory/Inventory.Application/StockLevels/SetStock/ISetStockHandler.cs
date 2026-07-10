using Inventory.Application.Contracts;
using SharedKernel;

namespace Inventory.Application.StockLevels.SetStock;

/// <summary>Stok oluşturma / güncelleme işlemini yürüten handler arabirimi.</summary>
public interface ISetStockHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<StockLevelDto>> ExecuteAsync(
        SetStockCommand command,
        CancellationToken cancellationToken = default);
}
