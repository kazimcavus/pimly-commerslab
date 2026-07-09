using SharedKernel;

namespace Pricing.Application.ItemPrices.DeleteItemPricesForItem;

/// <summary>Kaleme ait tüm fiyatları silme işlemini tanımlayan handler arabirimi.</summary>
public interface IDeleteItemPricesForItemHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(
        DeleteItemPricesForItemCommand command,
        CancellationToken cancellationToken = default);
}
