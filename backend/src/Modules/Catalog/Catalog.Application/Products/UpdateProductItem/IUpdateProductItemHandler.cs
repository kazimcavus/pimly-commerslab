using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Products.UpdateProductItem;

/// <summary>Ürün varyantı güncelleme işlemini yürüten handler arabirimi.</summary>
public interface IUpdateProductItemHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ProductItemDto>> ExecuteAsync(
        UpdateProductItemCommand command,
        CancellationToken cancellationToken = default);
}
