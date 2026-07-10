using Inventory.Domain.StockLevels;

namespace Inventory.Application.Contracts;

/// <summary>Inventory domain modelleri ile DTO'lar arasında dönüşüm sağlar.</summary>
internal static class InventoryMappings
{
    public static StockLevelDto ToDto(this StockLevel stockLevel) =>
        new(stockLevel.ProductItemId, stockLevel.Quantity, stockLevel.UpdatedAt);
}
