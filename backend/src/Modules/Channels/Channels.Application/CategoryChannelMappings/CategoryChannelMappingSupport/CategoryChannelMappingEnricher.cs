using Channels.Application.CategoryChannelMappings.Catalog;
using Channels.Application.Contracts;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;

namespace Channels.Application.CategoryChannelMappings.CategoryChannelMappingSupport;

/// <summary>CategoryChannelMapping DTO zenginleştirme yardımcıları.</summary>
internal static class CategoryChannelMappingEnricher
{
    internal static async Task<CategoryChannelMappingDto> EnrichAsync(
        CategoryChannelMapping mapping,
        IExternalCategoryRepository externalCategories,
        ICatalogCategoryGateway catalogCategories,
        CancellationToken cancellationToken)
    {
        var catalogCategory = await catalogCategories.GetByIdAsync(mapping.CatalogCategoryId, cancellationToken);
        var externalCategory = await externalCategories.GetByExternalIdAsync(
            mapping.Marketplace,
            mapping.ExternalId,
            cancellationToken);

        return mapping.ToDto(catalogCategory, externalCategory);
    }

    internal static async Task<IReadOnlyList<CategoryChannelMappingDto>> EnrichManyAsync(
        IReadOnlyList<CategoryChannelMapping> mappings,
        IExternalCategoryRepository externalCategories,
        ICatalogCategoryGateway catalogCategories,
        CancellationToken cancellationToken)
    {
        var results = new List<CategoryChannelMappingDto>(mappings.Count);

        foreach (var mapping in mappings)
        {
            results.Add(await EnrichAsync(
                mapping,
                externalCategories,
                catalogCategories,
                cancellationToken));
        }

        return results;
    }
}
