namespace Pricing.Application.ItemPrices.DeleteItemPrice;

/// <summary>Kalem fiyatı silme komutu.</summary>
public sealed record DeleteItemPriceCommand(Guid ProductItemId, Guid PriceDefinitionId);
