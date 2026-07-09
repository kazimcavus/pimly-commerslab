using SharedKernel;

namespace Channels.Domain.AttributeChannelMappings;

public interface IAttributeChannelMappingRepository
{
    Task<AttributeChannelMapping?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AttributeChannelMapping?> GetAsync(
        Marketplace marketplace,
        Guid catalogCategoryId,
        AttributeMappingSourceType sourceType,
        Guid catalogSourceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttributeChannelMapping>> ListAsync(
        Marketplace marketplace,
        Guid catalogCategoryId,
        AttributeMappingSourceType? sourceType,
        Pagination pagination,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Marketplace marketplace,
        Guid catalogCategoryId,
        AttributeMappingSourceType? sourceType,
        CancellationToken cancellationToken = default);

    Task<string?> ResolveExternalAttributeIdAsync(
        Marketplace marketplace,
        Guid catalogCategoryId,
        AttributeMappingSourceType sourceType,
        Guid catalogSourceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(AttributeChannelMapping mapping, CancellationToken cancellationToken = default);

    void Update(AttributeChannelMapping mapping);

    void Remove(AttributeChannelMapping mapping);
}
