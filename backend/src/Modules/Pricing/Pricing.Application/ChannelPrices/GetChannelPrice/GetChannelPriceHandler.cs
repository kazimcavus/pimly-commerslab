using Pricing.Application.Contracts;
using Pricing.Domain.ChannelPrices;
using SharedKernel;

namespace Pricing.Application.ChannelPrices.GetChannelPrice;

/// <summary>Kalemin bir pazaryerindeki kanal fiyatını getiren handler.</summary>
public sealed class GetChannelPriceHandler(IChannelPriceRepository channelPrices) : IGetChannelPriceHandler
{
    /// <inheritdoc/>
    public async Task<Result<ChannelPriceDto>> ExecuteAsync(
        GetChannelPriceQuery query,
        CancellationToken cancellationToken = default)
    {
        var marketplaceResult = Marketplace.FromCode(query.Marketplace);
        if (marketplaceResult.IsFailure)
        {
            return Result.Failure<ChannelPriceDto>(marketplaceResult.Error);
        }

        var channelPrice = await channelPrices.GetAsync(query.ProductItemId, marketplaceResult.Value, cancellationToken);
        return channelPrice is null
            ? Result.Failure<ChannelPriceDto>(Error.NotFound("Channel price not found."))
            : Result.Success(channelPrice.ToDto());
    }
}
