using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Channels.Infrastructure.Repositories;

internal sealed class AttributeChannelMappingRepository(ChannelsDbContext db) : IAttributeChannelMappingRepository
{
    public Task<AttributeChannelMapping?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.AttributeChannelMappings.FirstOrDefaultAsync(mapping => mapping.Id == id, cancellationToken);

    public Task<AttributeChannelMapping?> GetAsync(
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        AttributeMappingSourceType sourceType,
        Guid catalogSourceId,
        CancellationToken cancellationToken = default) =>
        db.AttributeChannelMappings.FirstOrDefaultAsync(
            mapping =>
                mapping.MarketplaceKey == marketplaceKey
                && mapping.CatalogCategoryId == catalogCategoryId
                && mapping.SourceType == sourceType
                && mapping.CatalogSourceId == catalogSourceId,
            cancellationToken);

    public async Task<IReadOnlyList<AttributeChannelMapping>> ListAsync(
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        AttributeMappingSourceType? sourceType,
        Pagination pagination,
        CancellationToken cancellationToken = default)
    {
        var query = Filter(db.AttributeChannelMappings, marketplaceKey, catalogCategoryId, sourceType)
            .OrderBy(mapping => mapping.SourceType)
            .ThenBy(mapping => mapping.CatalogSourceId);

        return await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        AttributeMappingSourceType? sourceType,
        CancellationToken cancellationToken = default) =>
        Filter(db.AttributeChannelMappings, marketplaceKey, catalogCategoryId, sourceType)
            .CountAsync(cancellationToken);

    public async Task<string?> ResolveExternalAttributeIdAsync(
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        AttributeMappingSourceType sourceType,
        Guid catalogSourceId,
        CancellationToken cancellationToken = default)
    {
        var mapping = await GetAsync(marketplaceKey, catalogCategoryId, sourceType, catalogSourceId, cancellationToken);
        return mapping?.ExternalAttributeId;
    }

    public async Task AddAsync(AttributeChannelMapping mapping, CancellationToken cancellationToken = default) =>
        await db.AttributeChannelMappings.AddAsync(mapping, cancellationToken);

    public void Update(AttributeChannelMapping mapping) =>
        db.AttributeChannelMappings.Update(mapping);

    public void Remove(AttributeChannelMapping mapping) =>
        db.AttributeChannelMappings.Remove(mapping);

    private static IQueryable<AttributeChannelMapping> Filter(
        IQueryable<AttributeChannelMapping> query,
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        AttributeMappingSourceType? sourceType)
    {
        query = query.Where(mapping =>
            mapping.MarketplaceKey == marketplaceKey
            && mapping.CatalogCategoryId == catalogCategoryId);

        if (sourceType.HasValue)
        {
            query = query.Where(mapping => mapping.SourceType == sourceType.Value);
        }

        return query;
    }
}
