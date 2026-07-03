using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Channels.Infrastructure.Repositories;

/// <summary>ExternalCategory cache kayıtları için EF Core tabanlı depo.</summary>
internal sealed class ExternalCategoryRepository(ChannelsDbContext db) : IExternalCategoryRepository
{
    public Task<int> CountByMarketplaceAsync(
        MarketplaceKey marketplaceKey,
        CancellationToken cancellationToken = default) =>
        db.ExternalCategories.CountAsync(
            category => category.MarketplaceKey == marketplaceKey,
            cancellationToken);

    public Task<ExternalCategory?> GetByExternalIdAsync(
        MarketplaceKey marketplaceKey,
        string externalId,
        CancellationToken cancellationToken = default) =>
        db.ExternalCategories.FirstOrDefaultAsync(
            category =>
                category.MarketplaceKey == marketplaceKey
                && category.ExternalId == externalId,
            cancellationToken);

    public async Task<IReadOnlyList<ExternalCategory>> SearchAsync(
        MarketplaceKey marketplaceKey,
        string? query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var categories = db.ExternalCategories
            .Where(category => category.MarketplaceKey == marketplaceKey);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            categories = categories.Where(category =>
                EF.Functions.ILike(category.Name, pattern)
                || EF.Functions.ILike(category.Path, pattern));
        }

        return await categories
            .OrderBy(category => category.Path)
            .ThenBy(category => category.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertBatchAsync(
        MarketplaceKey marketplaceKey,
        IReadOnlyList<ExternalCategoryUpsert> categories,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken = default)
    {
        if (categories.Count == 0)
        {
            return;
        }

        var externalIds = categories.Select(category => category.ExternalId).ToList();
        var existing = await db.ExternalCategories
            .Where(category =>
                category.MarketplaceKey == marketplaceKey
                && externalIds.Contains(category.ExternalId))
            .ToDictionaryAsync(category => category.ExternalId, cancellationToken);

        foreach (var category in categories)
        {
            if (existing.TryGetValue(category.ExternalId, out var current))
            {
                current.Update(
                    category.Name,
                    category.ParentExternalId,
                    category.Path,
                    category.IsLeaf,
                    syncedAt);
                continue;
            }

            var createResult = ExternalCategory.Create(
                marketplaceKey,
                category.ExternalId,
                category.Name,
                category.ParentExternalId,
                category.Path,
                category.IsLeaf,
                syncedAt);

            if (createResult.IsSuccess)
            {
                await db.ExternalCategories.AddAsync(createResult.Value, cancellationToken);
            }
        }
    }
}
