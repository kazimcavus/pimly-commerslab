using Pricing.Application.Contracts;
using SharedKernel;

namespace Pricing.Application.ChannelPrices.GetChannelPrice;

/// <summary>Kanal fiyatı getirme işlemini tanımlayan handler arabirimi.</summary>
public interface IGetChannelPriceHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ChannelPriceDto>> ExecuteAsync(
        GetChannelPriceQuery query,
        CancellationToken cancellationToken = default);
}
