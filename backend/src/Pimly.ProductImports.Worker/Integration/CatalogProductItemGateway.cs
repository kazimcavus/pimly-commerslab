using Catalog.Application.Products.GetProductItem;
using Pricing.Application.ItemPrices.Catalog;

namespace Pimly.ProductImports.Worker.Integration;

/// <summary>
/// Pricing için Catalog kaleminin varlığını doğrulayan gateway implementasyonu (worker kompozisyonu).
/// API host'undaki eşdeğerinin worker karşılığıdır.
/// </summary>
internal sealed class CatalogProductItemGateway(IGetProductItemHandler getProductItem) : ICatalogProductItemGateway
{
    public async Task<bool> ExistsAsync(Guid productItemId, CancellationToken cancellationToken = default)
    {
        var result = await getProductItem.ExecuteAsync(new GetProductItemQuery(productItemId), cancellationToken);
        return result.IsSuccess;
    }
}
