using Pricing.Application.Contracts;
using SharedKernel;

namespace Pricing.Application.ItemPrices.UpsertItemPrice;

/// <summary>Kalem fiyatı oluşturma / güncelleme işlemini yürüten handler arabirimi.</summary>
public interface IUpsertItemPriceHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ItemPriceDto>> ExecuteAsync(
        UpsertItemPriceCommand command,
        CancellationToken cancellationToken = default);
}
