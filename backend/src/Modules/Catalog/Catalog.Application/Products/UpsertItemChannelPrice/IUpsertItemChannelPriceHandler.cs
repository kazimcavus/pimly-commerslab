using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Products.UpsertItemChannelPrice;

/// <summary>Kalem kanal fiyatı oluşturma / güncelleme işlemini yürüten handler arabirimi.</summary>
public interface IUpsertItemChannelPriceHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ChannelPriceDto>> ExecuteAsync(
        UpsertItemChannelPriceCommand command,
        CancellationToken cancellationToken = default);
}
