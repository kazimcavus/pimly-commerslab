using Catalog.Application.Products.GetProductItem;
using Inventory.Application.StockLevels.Catalog;

namespace Pimly.ProductImports.Worker.Integration;

/// <summary>
/// Inventory için Catalog kaleminin varlığını doğrulayan gateway implementasyonu (worker kompozisyonu).
/// </summary>
internal sealed class InventoryCatalogProductItemGateway(IGetProductItemHandler getProductItem)
    : ICatalogProductItemGateway
{
    public async Task<bool> ExistsAsync(Guid productItemId, CancellationToken cancellationToken = default)
    {
        var result = await getProductItem.ExecuteAsync(new GetProductItemQuery(productItemId), cancellationToken);
        return result.IsSuccess;
    }
}
