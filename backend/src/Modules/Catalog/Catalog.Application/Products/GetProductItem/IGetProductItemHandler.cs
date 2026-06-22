using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Products.GetProductItem;

/// <summary>Ürün varyantı getirme işlemini yürüten handler arabirimi.</summary>
public interface IGetProductItemHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ProductItemDto>> ExecuteAsync(
        GetProductItemQuery query,
        CancellationToken cancellationToken = default);
}
