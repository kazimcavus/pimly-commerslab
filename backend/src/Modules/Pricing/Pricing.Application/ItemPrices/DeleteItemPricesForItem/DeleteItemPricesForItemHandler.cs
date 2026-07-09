using Pricing.Domain;
using Pricing.Domain.BasePrices;
using Pricing.Domain.ItemPrices;
using SharedKernel;

namespace Pricing.Application.ItemPrices.DeleteItemPricesForItem;

/// <summary>
/// Kalem silindiğinde (ProductItemDeleted) o kaleme ait tüm Pricing kayıtlarını (fiyat tanımı bazlı
/// tutarlar + temel fiyat) temizleyen handler. Idempotenttir: kayıt yoksa sessizce başarı döner
/// (olay yeniden işlenebilir).
/// </summary>
public sealed class DeleteItemPricesForItemHandler(
    IItemPriceRepository itemPrices,
    IBasePriceRepository basePrices,
    IUnitOfWork unitOfWork) : IDeleteItemPricesForItemHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeleteItemPricesForItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var changed = false;

        var prices = await itemPrices.ListByItemAsync(command.ProductItemId, cancellationToken);
        foreach (var price in prices)
        {
            itemPrices.Remove(price);
            changed = true;
        }

        var basePrice = await basePrices.GetByItemAsync(command.ProductItemId, cancellationToken);
        if (basePrice is not null)
        {
            basePrices.Remove(basePrice);
            changed = true;
        }

        if (changed)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
