using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Application.ExternalCatalog;

/// <summary>Pazaryeri kategori attribute API istemcisi.</summary>
public interface IMarketplaceCategoryAttributesClient
{
    Task<Result<IReadOnlyList<MarketplaceCategoryAttributeNode>>> FetchCategoryAttributesAsync(
        Marketplace marketplace,
        string externalCategoryId,
        CancellationToken cancellationToken = default);
}
