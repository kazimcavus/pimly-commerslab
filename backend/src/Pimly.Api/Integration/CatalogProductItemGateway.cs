using Catalog.Application.Products.GetProductItem;
using Pricing.Application.ItemPrices.Catalog;

namespace Pimly.Api.Integration;

/// <summary>Pricing için Catalog kaleminin varlığını doğrulayan gateway implementasyonu.</summary>
internal sealed class CatalogProductItemGateway(IGetProductItemHandler getProductItem) : ICatalogProductItemGateway
{
    public async Task<bool> ExistsAsync(Guid productItemId, CancellationToken cancellationToken = default)
    {
        var result = await getProductItem.ExecuteAsync(new GetProductItemQuery(productItemId), cancellationToken);
        return result.IsSuccess;
    }
}
