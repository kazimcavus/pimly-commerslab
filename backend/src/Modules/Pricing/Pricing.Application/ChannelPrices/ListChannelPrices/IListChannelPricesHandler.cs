using Pricing.Application.Contracts;
using SharedKernel;

namespace Pricing.Application.ChannelPrices.ListChannelPrices;

/// <summary>Kalemin kanal fiyatlarını listeleme işlemini tanımlayan handler arabirimi.</summary>
public interface IListChannelPricesHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<IReadOnlyList<ChannelPriceDto>>> ExecuteAsync(
        ListChannelPricesQuery query,
        CancellationToken cancellationToken = default);
}
