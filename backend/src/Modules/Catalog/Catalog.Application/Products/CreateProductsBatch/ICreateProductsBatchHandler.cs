using Catalog.Application.Products.CreateProductsBatch;
using SharedKernel;

namespace Catalog.Application.Products.CreateProductsBatch;

/// <summary>Toplu ürün oluşturma işlemini yürüten handler arabirimi.</summary>
public interface ICreateProductsBatchHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<CreateProductsBatchResult>> ExecuteAsync(
        CreateProductsBatchCommand command,
        CancellationToken cancellationToken = default);
}
