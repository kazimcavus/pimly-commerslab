using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Products.ListItemChannelPrices;

/// <summary>Kalemin kanal fiyatlarını listeleme işlemini yürüten handler arabirimi.</summary>
public interface IListItemChannelPricesHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<IReadOnlyList<ChannelPriceDto>>> ExecuteAsync(
        ListItemChannelPricesQuery query,
        CancellationToken cancellationToken = default);
}
