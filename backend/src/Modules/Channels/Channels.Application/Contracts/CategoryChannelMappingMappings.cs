using Channels.Application.CategoryChannelMappings.Catalog;
using Channels.Application.Contracts;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;

namespace Channels.Application.Contracts;

/// <summary>CategoryChannelMapping domain modelleri ile DTO'lar arasında dönüşüm sağlar.</summary>
internal static class CategoryChannelMappingMappings
{
    internal static CatalogCategorySnapshotDto ToDto(this CatalogCategorySnapshot snapshot) =>
        new(snapshot.Id, snapshot.Name, snapshot.Code);

    internal static ExternalCategorySummaryDto ToSummaryDto(this ExternalCategory category) =>
        new(
            category.ExternalId,
            category.Name,
            category.Path,
            category.IsLeaf,
            category.SyncedAt);

    internal static CategoryChannelMappingDto ToDto(
        this CategoryChannelMapping mapping,
        CatalogCategorySnapshot? catalogCategory,
        ExternalCategory? externalCategory) =>
        new(
            mapping.Id,
            mapping.CatalogCategoryId,
            mapping.Marketplace.Code,
            mapping.ExternalId,
            catalogCategory?.ToDto(),
            externalCategory?.ToSummaryDto());
}
