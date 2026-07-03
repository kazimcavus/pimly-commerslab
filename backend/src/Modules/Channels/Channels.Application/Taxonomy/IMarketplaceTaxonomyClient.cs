using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Application.Taxonomy;

/// <summary>Pazaryeri taksonomi verisini harici API'den çeker.</summary>
public interface IMarketplaceTaxonomyClient
{
    /// <summary>Tüm kategori ağacını getirir.</summary>
    Task<Result<IReadOnlyList<MarketplaceCategoryNode>>> FetchAllCategoriesAsync(
        MarketplaceDefinition marketplace,
        CancellationToken cancellationToken = default);
}
