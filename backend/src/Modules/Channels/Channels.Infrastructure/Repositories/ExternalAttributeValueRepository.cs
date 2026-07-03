using Channels.Domain.Marketplaces;
using Channels.Domain.Taxonomy;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Channels.Infrastructure.Repositories;

internal sealed class ExternalAttributeValueRepository(ChannelsDbContext db) : IExternalAttributeValueRepository
{
    public Task<ExternalAttributeValue?> GetAsync(
        MarketplaceKey marketplaceKey,
        string externalCategoryId,
        string externalAttributeId,
        string externalValueId,
        CancellationToken cancellationToken = default) =>
        db.ExternalAttributeValues.FirstOrDefaultAsync(
            value =>
                value.MarketplaceKey == marketplaceKey
                && value.ExternalCategoryId == externalCategoryId
                && value.ExternalAttributeId == externalAttributeId
                && value.ExternalValueId == externalValueId,
            cancellationToken);

    public async Task<IReadOnlyList<ExternalAttributeValue>> ListByAttributeAsync(
        MarketplaceKey marketplaceKey,
        string externalCategoryId,
        string externalAttributeId,
        CancellationToken cancellationToken = default) =>
        await db.ExternalAttributeValues
            .Where(value =>
                value.MarketplaceKey == marketplaceKey
                && value.ExternalCategoryId == externalCategoryId
                && value.ExternalAttributeId == externalAttributeId)
            .OrderBy(value => value.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ExternalAttributeValue>> ListByCategoryAsync(
        MarketplaceKey marketplaceKey,
        string externalCategoryId,
        CancellationToken cancellationToken = default) =>
        await db.ExternalAttributeValues
            .Where(value =>
                value.MarketplaceKey == marketplaceKey
                && value.ExternalCategoryId == externalCategoryId)
            .OrderBy(value => value.ExternalAttributeId)
            .ThenBy(value => value.Name)
            .ToListAsync(cancellationToken);
}
