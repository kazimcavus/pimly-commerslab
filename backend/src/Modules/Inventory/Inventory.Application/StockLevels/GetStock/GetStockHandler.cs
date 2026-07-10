using Inventory.Application.Contracts;
using Inventory.Domain.StockLevels;
using SharedKernel;

namespace Inventory.Application.StockLevels.GetStock;

/// <summary>Kalemin stok seviyesini getiren handler.</summary>
public sealed class GetStockHandler(IStockLevelRepository stockLevels) : IGetStockHandler
{
    /// <inheritdoc/>
    public async Task<Result<StockLevelDto>> ExecuteAsync(
        GetStockQuery query,
        CancellationToken cancellationToken = default)
    {
        var stockLevel = await stockLevels.GetByItemAsync(query.ProductItemId, cancellationToken);
        return stockLevel is null
            ? Result.Failure<StockLevelDto>(Error.NotFound("Stock level not found."))
            : Result.Success(stockLevel.ToDto());
    }
}
