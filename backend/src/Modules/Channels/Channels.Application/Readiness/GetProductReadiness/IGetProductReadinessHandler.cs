using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.Readiness.GetProductReadiness;

/// <summary>Ürün kanal hazırlık sorgusunu tanımlayan handler arayüzü.</summary>
public interface IGetProductReadinessHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ProductReadinessDto>> ExecuteAsync(
        GetProductReadinessQuery query,
        CancellationToken cancellationToken = default);
}
