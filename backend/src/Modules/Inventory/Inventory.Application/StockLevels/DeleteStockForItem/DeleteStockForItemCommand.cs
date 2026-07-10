namespace Inventory.Application.StockLevels.DeleteStockForItem;

/// <summary>Bir satılabilir kaleme ait stok kaydını silme komutu (kalem silindiğinde tetiklenir).</summary>
public sealed record DeleteStockForItemCommand(Guid ProductItemId);
