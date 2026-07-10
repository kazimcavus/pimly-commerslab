using Inventory.Application.Contracts;
using SharedKernel;

namespace Inventory.Application.StockLevels.GetStock;

/// <summary>Stok getirme işlemini tanımlayan handler arabirimi.</summary>
public interface IGetStockHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<StockLevelDto>> ExecuteAsync(
        GetStockQuery query,
        CancellationToken cancellationToken = default);
}
