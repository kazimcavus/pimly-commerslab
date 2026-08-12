using Channels.Application.Publications;
using Pricing.Application.ChannelPrices.ListChannelPricesForMarketplace;
using SharedKernel;

namespace Pimly.Integration;

/// <summary>
/// Channels yayınının Pricing'den kararlaştırılmış kanal fiyatlarını okuduğu ACL gateway impl'i.
/// Pricing'in liste handler'ına delege eder.
/// </summary>
public sealed class PricingChannelPriceGateway(IListChannelPricesForMarketplaceHandler listChannelPrices)
    : IPricingChannelPriceGateway
{
    public async Task<IReadOnlyList<DecidedChannelPrice>> ListForMarketplaceAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default)
    {
        var result = await listChannelPrices.ExecuteAsync(
            new ListChannelPricesForMarketplaceQuery(marketplace.Code),
            cancellationToken);

        if (result.IsFailure)
        {
            return [];
        }

        return result.Value
            .Select(price => new DecidedChannelPrice(
                price.ProductItemId,
                price.Amount,
                price.CompareAtAmount,
                price.Currency))
            .ToList();
    }
}
