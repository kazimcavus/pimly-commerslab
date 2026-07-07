using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Brands.UpdateBrand;

/// <summary>Marka güncelleme işlemini tanımlayan handler arayüzü.</summary>
public interface IUpdateBrandHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<BrandDto>> ExecuteAsync(UpdateBrandCommand command, CancellationToken cancellationToken = default);
}
