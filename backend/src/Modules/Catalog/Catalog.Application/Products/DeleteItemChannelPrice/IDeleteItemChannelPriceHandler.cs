using SharedKernel;

namespace Catalog.Application.Products.DeleteItemChannelPrice;

/// <summary>Kalem kanal fiyatı silme işlemini yürüten handler arabirimi.</summary>
public interface IDeleteItemChannelPriceHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(
        DeleteItemChannelPriceCommand command,
        CancellationToken cancellationToken = default);
}
