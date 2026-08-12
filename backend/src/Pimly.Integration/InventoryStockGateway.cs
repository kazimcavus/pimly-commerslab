using Channels.Application.Listings.OfferSync;
using Inventory.Domain.StockLevels;

namespace Pimly.Integration;

/// <summary>
/// Channels senkronunun Inventory'den stok okuduğu ACL gateway impl'i. Stok deposuna delege eder;
/// kaydı olmayan kalem sonuçta yer almaz (çağıran tarafta 0 sayılır).
/// </summary>
public sealed class InventoryStockGateway(IStockLevelRepository stockLevels) : IInventoryStockGateway
{
    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<Guid, int>> GetQuantitiesAsync(
        IReadOnlyCollection<Guid> productItemIds,
        CancellationToken cancellationToken = default)
    {
        var levels = await stockLevels.ListByItemsAsync(productItemIds, cancellationToken);

        return levels.ToDictionary(level => level.ProductItemId, level => level.Quantity);
    }
}
