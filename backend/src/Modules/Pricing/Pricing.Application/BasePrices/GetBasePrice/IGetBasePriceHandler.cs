using Pricing.Application.Contracts;
using SharedKernel;

namespace Pricing.Application.BasePrices.GetBasePrice;

/// <summary>Temel fiyat getirme işlemini tanımlayan handler arabirimi.</summary>
public interface IGetBasePriceHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<BasePriceDto>> ExecuteAsync(
        GetBasePriceQuery query,
        CancellationToken cancellationToken = default);
}
