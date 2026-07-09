using Pricing.Application.Contracts;
using Pricing.Domain.ChannelPrices;
using SharedKernel;

namespace Pricing.Application.ChannelPrices.ListChannelPrices;

/// <summary>Kalemin tüm pazaryeri kanal fiyatlarını listeleyen handler.</summary>
public sealed class ListChannelPricesHandler(IChannelPriceRepository channelPrices) : IListChannelPricesHandler
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ChannelPriceDto>>> ExecuteAsync(
        ListChannelPricesQuery query,
        CancellationToken cancellationToken = default)
    {
        var prices = await channelPrices.ListByItemAsync(query.ProductItemId, cancellationToken);
        return Result.Success<IReadOnlyList<ChannelPriceDto>>(
            prices.Select(price => price.ToDto()).ToList());
    }
}
