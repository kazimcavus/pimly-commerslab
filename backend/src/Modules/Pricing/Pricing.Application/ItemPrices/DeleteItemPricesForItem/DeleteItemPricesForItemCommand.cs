namespace Pricing.Application.ItemPrices.DeleteItemPricesForItem;

/// <summary>Bir satılabilir kaleme ait tüm fiyat kayıtlarını silme komutu (kalem silindiğinde tetiklenir).</summary>
public sealed record DeleteItemPricesForItemCommand(Guid ProductItemId);
