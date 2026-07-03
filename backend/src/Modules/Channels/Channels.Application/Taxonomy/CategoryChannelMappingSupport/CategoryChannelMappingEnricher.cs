using Channels.Application.Contracts;
using Channels.Application.Ports;
using Channels.Domain.Taxonomy;

namespace Channels.Application.Taxonomy.CategoryChannelMappingSupport;

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
            mapping.MarketplaceKey,
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
