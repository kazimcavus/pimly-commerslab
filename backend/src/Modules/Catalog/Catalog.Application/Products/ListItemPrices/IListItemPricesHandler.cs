using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Products.ListItemPrices;

/// <summary>Kalemin fiyat tanımı bazlı fiyatlarını listeleme işlemini yürüten handler arabirimi.</summary>
public interface IListItemPricesHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<IReadOnlyList<ItemPriceDto>>> ExecuteAsync(
        ListItemPricesQuery query,
        CancellationToken cancellationToken = default);
}
