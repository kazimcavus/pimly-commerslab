using Catalog.Application.Products.GetProductItem;
using Pricing.Application.ItemPrices.Catalog;

namespace Pimly.Integration;

/// <summary>Pricing için Catalog kaleminin varlığını doğrulayan gateway implementasyonu.</summary>
public sealed class PricingCatalogProductItemGateway(IGetProductItemHandler getProductItem) : ICatalogProductItemGateway
{
    public async Task<bool> ExistsAsync(Guid productItemId, CancellationToken cancellationToken = default)
    {
        var result = await getProductItem.ExecuteAsync(new GetProductItemQuery(productItemId), cancellationToken);
        return result.IsSuccess;
    }
}
