using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Brands.ListBrands;

/// <summary>Marka listeleme işlemini tanımlayan handler arayüzü.</summary>
public interface IListBrandsHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<PagedResult<BrandDto>>> ExecuteAsync(
        ListBrandsQuery query,
        CancellationToken cancellationToken = default);
}
