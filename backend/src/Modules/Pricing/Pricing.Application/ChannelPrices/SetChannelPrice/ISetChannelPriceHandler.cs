using Pricing.Application.Contracts;
using SharedKernel;

namespace Pricing.Application.ChannelPrices.SetChannelPrice;

/// <summary>Kanal fiyatı oluşturma / güncelleme işlemini yürüten handler arabirimi.</summary>
public interface ISetChannelPriceHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ChannelPriceDto>> ExecuteAsync(
        SetChannelPriceCommand command,
        CancellationToken cancellationToken = default);
}
