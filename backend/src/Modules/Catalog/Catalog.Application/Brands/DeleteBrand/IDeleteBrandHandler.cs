using SharedKernel;

namespace Catalog.Application.Brands.DeleteBrand;

/// <summary>Marka silme işlemini tanımlayan handler arayüzü.</summary>
public interface IDeleteBrandHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(DeleteBrandCommand command, CancellationToken cancellationToken = default);
}
