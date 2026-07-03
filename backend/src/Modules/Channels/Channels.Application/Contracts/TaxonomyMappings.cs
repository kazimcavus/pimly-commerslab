using Channels.Application.Contracts;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.Marketplaces;
using Channels.Domain.TaxonomySync;

namespace Channels.Application.Contracts;

/// <summary>Taxonomy domain modelleri ile DTO'lar arasında dönüşüm sağlar.</summary>
internal static class TaxonomyMappings
{
    internal static TaxonomySyncRunDto ToDto(this TaxonomySyncRun syncRun, Marketplace marketplace) =>
        new(
            syncRun.Id,
            marketplace.Code,
            syncRun.Status.ToString().ToLowerInvariant(),
            syncRun.CreatedAt,
            syncRun.StartedAt,
            syncRun.CompletedAt,
            syncRun.ProcessedCount,
            syncRun.TotalEstimate,
            syncRun.ErrorMessage);

    internal static ExternalCategoryDto ToDto(this ExternalCategory category) =>
        new(
            category.Id,
            category.ExternalId,
            category.Name,
            category.ParentExternalId,
            category.Path,
            category.IsLeaf,
            category.SyncedAt);
}
