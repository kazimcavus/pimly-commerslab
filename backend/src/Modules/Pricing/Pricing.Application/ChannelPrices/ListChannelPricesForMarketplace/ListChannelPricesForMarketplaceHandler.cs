using Pricing.Application.Contracts;
using Pricing.Domain.ChannelPrices;
using SharedKernel;

namespace Pricing.Application.ChannelPrices.ListChannelPricesForMarketplace;

/// <summary>Bir pazaryerindeki tüm kanal fiyatlarını listeleyen handler (yayın kaynağı).</summary>
public sealed class ListChannelPricesForMarketplaceHandler(IChannelPriceRepository channelPrices)
    : IListChannelPricesForMarketplaceHandler
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ChannelPriceDto>>> ExecuteAsync(
        ListChannelPricesForMarketplaceQuery query,
        CancellationToken cancellationToken = default)
    {
        var marketplaceResult = Marketplace.FromCode(query.Marketplace);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ChannelPriceDto>>(marketplaceResult.Error);
        }

        var prices = await channelPrices.ListByMarketplaceAsync(marketplaceResult.Value, cancellationToken);
        return Result.Success<IReadOnlyList<ChannelPriceDto>>(
            prices.Select(price => price.ToDto()).ToList());
    }
}
