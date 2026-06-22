using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Products.UpdateProduct;

/// <summary>Ürün güncelleme işlemini yürüten handler arabirimi.</summary>
public interface IUpdateProductHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ProductDto>> ExecuteAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default);
}
