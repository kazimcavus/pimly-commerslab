using Pricing.Application.Contracts;
using SharedKernel;

namespace Pricing.Application.ChannelPrices.ListChannelPricesForMarketplace;

/// <summary>Pazaryeri bazlı kanal fiyatı listeleme işlemini tanımlayan handler arabirimi.</summary>
public interface IListChannelPricesForMarketplaceHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<IReadOnlyList<ChannelPriceDto>>> ExecuteAsync(
        ListChannelPricesForMarketplaceQuery query,
        CancellationToken cancellationToken = default);
}
