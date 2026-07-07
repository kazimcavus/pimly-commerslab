using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Brands.GetBrand;

/// <summary>Marka getirme işlemini tanımlayan handler arayüzü.</summary>
public interface IGetBrandHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<BrandDto>> ExecuteAsync(GetBrandQuery query, CancellationToken cancellationToken = default);
}
