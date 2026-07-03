namespace Catalog.Application.Products.UpsertItemChannelPrice;

/// <summary>Kalem kanal fiyatı oluşturma / güncelleme komutu.</summary>
public sealed record UpsertItemChannelPriceCommand(
    Guid ProductItemId,
    string MarketplaceKey,
    decimal Price,
    decimal? CompareAtPrice,
    string? Currency = null);
