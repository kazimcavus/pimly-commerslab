using SharedKernel;

namespace Catalog.Application.Products.DeleteProductItem;

/// <summary>Ürün varyantı silme işlemini yürüten handler arabirimi.</summary>
public interface IDeleteProductItemHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(
        DeleteProductItemCommand command,
        CancellationToken cancellationToken = default);
}
