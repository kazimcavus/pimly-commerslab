using Pricing.Domain.BasePrices;
using Pricing.Domain.ChannelPrices;
using Pricing.Domain.ItemPrices;
using Pricing.Domain.PriceDefinitions;

namespace Pricing.Application.Contracts;

/// <summary>Pricing domain modelleri ile DTO'lar arasında dönüşüm sağlar.</summary>
internal static class PricingMappings
{
    public static ChannelPriceDto ToDto(this ChannelPrice channelPrice) =>
        new(
            channelPrice.ProductItemId,
            channelPrice.Marketplace.Code,
            channelPrice.Amount,
            channelPrice.CompareAtAmount,
            channelPrice.Currency,
            channelPrice.UpdatedAt);

    public static PriceDefinitionDto ToDto(this PriceDefinition definition) =>
        new(definition.Id, definition.Name, definition.Code);

    public static BasePriceDto ToDto(this BasePrice basePrice) =>
        new(
            basePrice.ProductItemId,
            basePrice.Amount,
            basePrice.CompareAtAmount,
            basePrice.Currency,
            basePrice.UpdatedAt);

    public static ItemPriceDto ToDto(this ProductItemPrice itemPrice, string definitionName) =>
        new(
            itemPrice.Id,
            itemPrice.ProductItemId,
            itemPrice.PriceDefinitionId,
            definitionName,
            itemPrice.Amount,
            itemPrice.Currency,
            itemPrice.UpdatedAt);
}
