using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Products.CreateProduct;

/// <summary>Yeni ürün oluşturma işlemini yürüten handler arabirimi.</summary>
public interface ICreateProductHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ProductDto>> ExecuteAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default);
}
