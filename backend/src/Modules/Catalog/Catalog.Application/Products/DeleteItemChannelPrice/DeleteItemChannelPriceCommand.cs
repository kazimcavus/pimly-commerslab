namespace Catalog.Application.Products.DeleteItemChannelPrice;

/// <summary>Kalem kanal fiyatı silme komutu.</summary>
public sealed record DeleteItemChannelPriceCommand(Guid ProductItemId, string MarketplaceKey);
