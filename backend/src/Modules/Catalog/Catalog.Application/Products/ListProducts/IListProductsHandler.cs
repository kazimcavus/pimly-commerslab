using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Products.ListProducts;

/// <summary>Ürün listeleme işlemini tanımlayan sözleşme.</summary>
public interface IListProductsHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<PagedResult<ProductDto>>> ExecuteAsync(
        ListProductsQuery query,
        CancellationToken cancellationToken = default);
}
