using SharedKernel;

namespace Catalog.Application.Products.DeleteProduct;

/// <summary>Ürün silme işlemini yürüten handler arabirimi.</summary>
public interface IDeleteProductHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(
        DeleteProductCommand command,
        CancellationToken cancellationToken = default);
}
