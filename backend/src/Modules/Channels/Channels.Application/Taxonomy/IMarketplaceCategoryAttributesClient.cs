using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Application.Taxonomy;

/// <summary>Pazaryeri kategori attribute API istemcisi.</summary>
public interface IMarketplaceCategoryAttributesClient
{
    Task<Result<IReadOnlyList<MarketplaceCategoryAttributeNode>>> FetchCategoryAttributesAsync(
        MarketplaceDefinition marketplace,
        string externalCategoryId,
        CancellationToken cancellationToken = default);
}
