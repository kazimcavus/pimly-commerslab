using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.Marketplaces;
using Channels.Domain.TaxonomySync;
using Channels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Channels.Infrastructure.Repositories;

internal sealed class ExternalCategoryAttributeRepository(ChannelsDbContext db) : IExternalCategoryAttributeRepository
{
    public Task<ExternalCategoryAttribute?> GetAsync(
        Marketplace marketplace,
        string externalCategoryId,
        string externalAttributeId,
        CancellationToken cancellationToken = default) =>
        db.ExternalCategoryAttributes.FirstOrDefaultAsync(
            attribute =>
                attribute.Marketplace == marketplace
                && attribute.ExternalCategoryId == externalCategoryId
                && attribute.ExternalAttributeId == externalAttributeId,
            cancellationToken);

    public async Task<IReadOnlyList<ExternalCategoryAttribute>> ListByCategoryAsync(
        Marketplace marketplace,
        string externalCategoryId,
        CancellationToken cancellationToken = default) =>
        await db.ExternalCategoryAttributes
            .Where(attribute =>
                attribute.Marketplace == marketplace
                && attribute.ExternalCategoryId == externalCategoryId)
            .OrderBy(attribute => attribute.Name)
            .ToListAsync(cancellationToken);

    public async Task UpsertBatchAsync(
        Marketplace marketplace,
        string externalCategoryId,
        IReadOnlyList<ExternalCategoryAttributeUpsert> attributes,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken = default)
    {
        if (attributes.Count == 0)
        {
            return;
        }

        var attributeIds = attributes.Select(attribute => attribute.ExternalAttributeId).ToList();
        var existingAttributes = await db.ExternalCategoryAttributes
            .Where(attribute =>
                attribute.Marketplace == marketplace
                && attribute.ExternalCategoryId == externalCategoryId
                && attributeIds.Contains(attribute.ExternalAttributeId))
            .ToDictionaryAsync(attribute => attribute.ExternalAttributeId, cancellationToken);

        foreach (var attribute in attributes)
        {
            if (existingAttributes.TryGetValue(attribute.ExternalAttributeId, out var current))
            {
                current.Update(
                    attribute.Name,
                    attribute.Required,
                    attribute.AllowCustom,
                    attribute.IsVariant,
                    syncedAt);
            }
            else
            {
                var createResult = ExternalCategoryAttribute.Create(
                    marketplace,
                    externalCategoryId,
                    attribute.ExternalAttributeId,
                    attribute.Name,
                    attribute.Required,
                    attribute.AllowCustom,
                    attribute.IsVariant,
                    syncedAt);

                if (createResult.IsSuccess)
                {
                    await db.ExternalCategoryAttributes.AddAsync(createResult.Value, cancellationToken);
                }
            }

            await UpsertValuesAsync(
                marketplace,
                externalCategoryId,
                attribute.ExternalAttributeId,
                attribute.Values,
                syncedAt,
                cancellationToken);
        }
    }

    private async Task UpsertValuesAsync(
        Marketplace marketplace,
        string externalCategoryId,
        string externalAttributeId,
        IReadOnlyList<ExternalAttributeValueUpsert> values,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken)
    {
        if (values.Count == 0)
        {
            return;
        }

        var valueIds = values.Select(value => value.ExternalValueId).ToList();
        var existingValues = await db.ExternalAttributeValues
            .Where(value =>
                value.Marketplace == marketplace
                && value.ExternalCategoryId == externalCategoryId
                && value.ExternalAttributeId == externalAttributeId
                && valueIds.Contains(value.ExternalValueId))
            .ToDictionaryAsync(value => value.ExternalValueId, cancellationToken);

        foreach (var value in values)
        {
            if (existingValues.TryGetValue(value.ExternalValueId, out var current))
            {
                current.Update(value.Name, syncedAt);
                continue;
            }

            var createResult = ExternalAttributeValue.Create(
                marketplace,
                externalCategoryId,
                externalAttributeId,
                value.ExternalValueId,
                value.Name,
                syncedAt);

            if (createResult.IsSuccess)
            {
                await db.ExternalAttributeValues.AddAsync(createResult.Value, cancellationToken);
            }
        }
    }
}
