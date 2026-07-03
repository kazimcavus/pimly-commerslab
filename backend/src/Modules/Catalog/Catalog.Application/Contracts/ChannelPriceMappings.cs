using Catalog.Domain.Products;

namespace Catalog.Application.Contracts;

/// <summary>ProductItemChannelPrice domain modeli ile DTO arasında dönüşüm sağlar.</summary>
internal static class ChannelPriceMappings
{
    public static ChannelPriceDto ToDto(this ProductItemChannelPrice channelPrice) =>
        new(
            channelPrice.Id,
            channelPrice.ProductItemId,
            channelPrice.MarketplaceKey,
            channelPrice.Price,
            channelPrice.CompareAtPrice,
            channelPrice.Currency,
            channelPrice.UpdatedAt);
}
