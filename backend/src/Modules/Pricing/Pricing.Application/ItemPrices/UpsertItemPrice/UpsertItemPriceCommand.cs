namespace Pricing.Application.ItemPrices.UpsertItemPrice;

/// <summary>Kalem fiyatı (fiyat tanımı bazlı tutar) oluşturma / güncelleme komutu.</summary>
public sealed record UpsertItemPriceCommand(
    Guid ProductItemId,
    Guid PriceDefinitionId,
    decimal Amount,
    string? Currency = null);
