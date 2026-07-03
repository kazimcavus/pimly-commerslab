using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.Marketplaces;
using Channels.Domain.TaxonomySync;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Channels.Infrastructure.Repositories;

internal sealed class ExternalAttributeValueRepository(ChannelsDbContext db) : IExternalAttributeValueRepository
{
    public Task<ExternalAttributeValue?> GetAsync(
        Marketplace marketplace,
        string externalCategoryId,
        string externalAttributeId,
        string externalValueId,
        CancellationToken cancellationToken = default) =>
        db.ExternalAttributeValues.FirstOrDefaultAsync(
            value =>
                value.Marketplace == marketplace
                && value.ExternalCategoryId == externalCategoryId
                && value.ExternalAttributeId == externalAttributeId
                && value.ExternalValueId == externalValueId,
            cancellationToken);

    public async Task<IReadOnlyList<ExternalAttributeValue>> ListByAttributeAsync(
        Marketplace marketplace,
        string externalCategoryId,
        string externalAttributeId,
        CancellationToken cancellationToken = default) =>
        await db.ExternalAttributeValues
            .Where(value =>
                value.Marketplace == marketplace
                && value.ExternalCategoryId == externalCategoryId
                && value.ExternalAttributeId == externalAttributeId)
            .OrderBy(value => value.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ExternalAttributeValue>> ListByCategoryAsync(
        Marketplace marketplace,
        string externalCategoryId,
        CancellationToken cancellationToken = default) =>
        await db.ExternalAttributeValues
            .Where(value =>
                value.Marketplace == marketplace
                && value.ExternalCategoryId == externalCategoryId)
            .OrderBy(value => value.ExternalAttributeId)
            .ThenBy(value => value.Name)
            .ToListAsync(cancellationToken);
}
