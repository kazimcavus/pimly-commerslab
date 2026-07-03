using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Channels.Infrastructure.Repositories;

/// <summary>CategoryChannelMapping aggregate'leri için EF Core tabanlı depo.</summary>
internal sealed class CategoryChannelMappingRepository(ChannelsDbContext db) : ICategoryChannelMappingRepository
{
    public Task<CategoryChannelMapping?> GetAsync(
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        CancellationToken cancellationToken = default) =>
        db.CategoryChannelMappings.FirstOrDefaultAsync(
            mapping =>
                mapping.MarketplaceKey == marketplaceKey
                && mapping.CatalogCategoryId == catalogCategoryId,
            cancellationToken);

    public async Task<IReadOnlyList<CategoryChannelMapping>> ListAsync(
        MarketplaceKey marketplaceKey,
        Guid? catalogCategoryId,
        Pagination pagination,
        CancellationToken cancellationToken = default)
    {
        var query = FilterByMarketplace(db.CategoryChannelMappings, marketplaceKey, catalogCategoryId)
            .OrderBy(mapping => mapping.CatalogCategoryId);

        return await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        MarketplaceKey marketplaceKey,
        Guid? catalogCategoryId,
        CancellationToken cancellationToken = default) =>
        FilterByMarketplace(db.CategoryChannelMappings, marketplaceKey, catalogCategoryId)
            .CountAsync(cancellationToken);

    public async Task<string?> ResolveExternalIdAsync(
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        CancellationToken cancellationToken = default)
    {
        var mapping = await GetAsync(marketplaceKey, catalogCategoryId, cancellationToken);
        return mapping?.ExternalId;
    }

    public async Task AddAsync(CategoryChannelMapping mapping, CancellationToken cancellationToken = default) =>
        await db.CategoryChannelMappings.AddAsync(mapping, cancellationToken);

    public void Update(CategoryChannelMapping mapping) =>
        db.CategoryChannelMappings.Update(mapping);

    public void Remove(CategoryChannelMapping mapping) =>
        db.CategoryChannelMappings.Remove(mapping);

    private static IQueryable<CategoryChannelMapping> FilterByMarketplace(
        IQueryable<CategoryChannelMapping> query,
        MarketplaceKey marketplaceKey,
        Guid? catalogCategoryId)
    {
        query = query.Where(mapping => mapping.MarketplaceKey == marketplaceKey);

        if (catalogCategoryId.HasValue)
        {
            query = query.Where(mapping => mapping.CatalogCategoryId == catalogCategoryId.Value);
        }

        return query;
    }
}
