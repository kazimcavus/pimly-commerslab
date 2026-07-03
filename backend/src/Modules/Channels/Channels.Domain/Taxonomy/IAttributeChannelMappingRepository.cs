using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Domain.Taxonomy;

public interface IAttributeChannelMappingRepository
{
    Task<AttributeChannelMapping?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AttributeChannelMapping?> GetAsync(
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        AttributeMappingSourceType sourceType,
        Guid catalogSourceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttributeChannelMapping>> ListAsync(
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        AttributeMappingSourceType? sourceType,
        Pagination pagination,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        AttributeMappingSourceType? sourceType,
        CancellationToken cancellationToken = default);

    Task<string?> ResolveExternalAttributeIdAsync(
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        AttributeMappingSourceType sourceType,
        Guid catalogSourceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(AttributeChannelMapping mapping, CancellationToken cancellationToken = default);

    void Update(AttributeChannelMapping mapping);

    void Remove(AttributeChannelMapping mapping);
}
