using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Domain.Taxonomy;

/// <summary>CategoryChannelMapping aggregate depo arabirimi.</summary>
public interface ICategoryChannelMappingRepository
{
    Task<CategoryChannelMapping?> GetAsync(
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryChannelMapping>> ListAsync(
        MarketplaceKey marketplaceKey,
        Guid? catalogCategoryId,
        Pagination pagination,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        MarketplaceKey marketplaceKey,
        Guid? catalogCategoryId,
        CancellationToken cancellationToken = default);

    Task<string?> ResolveExternalIdAsync(
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        CancellationToken cancellationToken = default);

    Task AddAsync(CategoryChannelMapping mapping, CancellationToken cancellationToken = default);

    void Update(CategoryChannelMapping mapping);

    void Remove(CategoryChannelMapping mapping);
}
