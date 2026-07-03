using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Application.ExternalCatalog;

/// <summary>Pazaryeri kategori attribute API istemcisi.</summary>
/// <remarks>Kimlik bilgisi gerektiren pazaryerlerinde istemci, kimliği bağlantı deposundan kendisi çözer.</remarks>
public interface IMarketplaceCategoryAttributesClient
{
    /// <summary>Kategorinin attribute tanımlarını getirir.</summary>
    Task<Result<IReadOnlyList<MarketplaceCategoryAttributeNode>>> FetchCategoryAttributesAsync(
        Marketplace marketplace,
        string externalCategoryId,
        CancellationToken cancellationToken = default);
}
