using Channels.Domain.Taxonomy;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Channels.Infrastructure.Repositories;

internal sealed class AttributeValueChannelMappingRepository(ChannelsDbContext db) : IAttributeValueChannelMappingRepository
{
    public Task<AttributeValueChannelMapping?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.AttributeValueChannelMappings.FirstOrDefaultAsync(mapping => mapping.Id == id, cancellationToken);

    public Task<AttributeValueChannelMapping?> GetAsync(
        Guid attributeChannelMappingId,
        Guid catalogValueId,
        CancellationToken cancellationToken = default) =>
        db.AttributeValueChannelMappings.FirstOrDefaultAsync(
            mapping =>
                mapping.AttributeChannelMappingId == attributeChannelMappingId
                && mapping.CatalogValueId == catalogValueId,
            cancellationToken);

    public async Task<IReadOnlyList<AttributeValueChannelMapping>> ListByFieldMappingAsync(
        Guid attributeChannelMappingId,
        CancellationToken cancellationToken = default) =>
        await db.AttributeValueChannelMappings
            .Where(mapping => mapping.AttributeChannelMappingId == attributeChannelMappingId)
            .OrderBy(mapping => mapping.CatalogValueId)
            .ToListAsync(cancellationToken);

    public async Task<string?> ResolveExternalValueIdAsync(
        Guid attributeChannelMappingId,
        Guid catalogValueId,
        CancellationToken cancellationToken = default)
    {
        var mapping = await GetAsync(attributeChannelMappingId, catalogValueId, cancellationToken);
        return mapping?.ExternalValueId;
    }

    public async Task AddAsync(AttributeValueChannelMapping mapping, CancellationToken cancellationToken = default) =>
        await db.AttributeValueChannelMappings.AddAsync(mapping, cancellationToken);

    public void Update(AttributeValueChannelMapping mapping) =>
        db.AttributeValueChannelMappings.Update(mapping);

    public void Remove(AttributeValueChannelMapping mapping) =>
        db.AttributeValueChannelMappings.Remove(mapping);

    public async Task RemoveByFieldMappingAsync(Guid attributeChannelMappingId, CancellationToken cancellationToken = default)
    {
        var mappings = await db.AttributeValueChannelMappings
            .Where(mapping => mapping.AttributeChannelMappingId == attributeChannelMappingId)
            .ToListAsync(cancellationToken);

        db.AttributeValueChannelMappings.RemoveRange(mappings);
    }
}
