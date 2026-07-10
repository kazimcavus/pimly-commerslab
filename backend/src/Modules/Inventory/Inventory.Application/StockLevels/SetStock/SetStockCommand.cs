namespace Inventory.Application.StockLevels.SetStock;

/// <summary>Kalemin stok miktarını oluşturma / güncelleme komutu.</summary>
public sealed record SetStockCommand(Guid ProductItemId, int Quantity);
