namespace Catalog.Application.Products.DeleteItemPrice;

/// <summary>Kalem fiyatı silme komutu.</summary>
public sealed record DeleteItemPriceCommand(Guid ProductItemId, Guid PriceDefinitionId);
