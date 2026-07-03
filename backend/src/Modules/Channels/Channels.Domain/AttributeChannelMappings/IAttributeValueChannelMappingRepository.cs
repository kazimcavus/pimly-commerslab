namespace Channels.Domain.AttributeChannelMappings;

public interface IAttributeValueChannelMappingRepository
{
    Task<AttributeValueChannelMapping?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AttributeValueChannelMapping?> GetAsync(
        Guid attributeChannelMappingId,
        Guid catalogValueId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttributeValueChannelMapping>> ListByFieldMappingAsync(
        Guid attributeChannelMappingId,
        CancellationToken cancellationToken = default);

    Task<string?> ResolveExternalValueIdAsync(
        Guid attributeChannelMappingId,
        Guid catalogValueId,
        CancellationToken cancellationToken = default);

    Task AddAsync(AttributeValueChannelMapping mapping, CancellationToken cancellationToken = default);

    void Update(AttributeValueChannelMapping mapping);

    void Remove(AttributeValueChannelMapping mapping);

    Task RemoveByFieldMappingAsync(Guid attributeChannelMappingId, CancellationToken cancellationToken = default);
}
