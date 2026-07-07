using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Brands.CreateBrand;

/// <summary>Yeni marka oluşturma işlemini tanımlayan handler arayüzü.</summary>
public interface ICreateBrandHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<BrandDto>> ExecuteAsync(CreateBrandCommand command, CancellationToken cancellationToken = default);
}
