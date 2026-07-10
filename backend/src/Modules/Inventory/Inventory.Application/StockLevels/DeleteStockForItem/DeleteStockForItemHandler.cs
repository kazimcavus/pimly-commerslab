using Inventory.Domain;
using Inventory.Domain.StockLevels;
using SharedKernel;

namespace Inventory.Application.StockLevels.DeleteStockForItem;

/// <summary>
/// Kalem silindiğinde (ProductItemDeleted) o kaleme ait stok kaydını temizleyen handler.
/// Idempotenttir: kayıt yoksa sessizce başarı döner (olay yeniden işlenebilir).
/// </summary>
public sealed class DeleteStockForItemHandler(
    IStockLevelRepository stockLevels,
    IUnitOfWork unitOfWork) : IDeleteStockForItemHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteStockForItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var stockLevel = await stockLevels.GetByItemAsync(command.ProductItemId, cancellationToken);
        if (stockLevel is null)
        {
            return Result.Success();
        }

        stockLevels.Remove(stockLevel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
