using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Application.ExternalCatalog;

/// <summary>Pazaryeri taksonomi verisini harici API'den çeker.</summary>
public interface IMarketplaceTaxonomyClient
{
    /// <summary>Tüm kategori ağacını getirir.</summary>
    Task<Result<IReadOnlyList<MarketplaceCategoryNode>>> FetchAllCategoriesAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default);
}
