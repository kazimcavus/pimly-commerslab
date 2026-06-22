using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Products.GetProduct;

/// <summary>Ürün getirme işlemini yürüten handler arabirimi.</summary>
public interface IGetProductHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ProductDto>> ExecuteAsync(
        GetProductQuery query,
        CancellationToken cancellationToken = default);
}
