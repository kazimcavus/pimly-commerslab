using Pricing.Application.Contracts;
using SharedKernel;

namespace Pricing.Application.BasePrices.SetBasePrice;

/// <summary>Temel fiyat oluşturma / güncelleme işlemini yürüten handler arabirimi.</summary>
public interface ISetBasePriceHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<BasePriceDto>> ExecuteAsync(
        SetBasePriceCommand command,
        CancellationToken cancellationToken = default);
}
