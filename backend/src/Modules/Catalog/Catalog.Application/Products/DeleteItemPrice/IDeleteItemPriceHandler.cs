using SharedKernel;

namespace Catalog.Application.Products.DeleteItemPrice;

/// <summary>Kalem fiyatı silme işlemini yürüten handler arabirimi.</summary>
public interface IDeleteItemPriceHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(
        DeleteItemPriceCommand command,
        CancellationToken cancellationToken = default);
}
