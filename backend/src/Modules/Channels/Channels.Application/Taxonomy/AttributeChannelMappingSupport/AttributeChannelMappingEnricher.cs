using Channels.Application.Contracts;
using Channels.Application.Ports;
using Channels.Domain.Taxonomy;

namespace Channels.Application.Taxonomy.AttributeChannelMappingSupport;

internal static class AttributeChannelMappingEnricher
{
    internal static async Task<AttributeChannelMappingDto> EnrichAsync(
        AttributeChannelMapping mapping,
        ICategoryChannelMappingRepository categoryMappings,
        IExternalCategoryAttributeRepository externalAttributes,
        ICatalogAttributeGateway catalogAttributes,
        ICatalogVariantGateway catalogVariants,
        CancellationToken cancellationToken)
    {
        var externalCategoryId = await categoryMappings.ResolveExternalIdAsync(
            mapping.MarketplaceKey,
            mapping.CatalogCategoryId,
            cancellationToken);

        ExternalCategoryAttribute? externalAttribute = null;
        if (externalCategoryId is not null)
        {
            externalAttribute = await externalAttributes.GetAsync(
                mapping.MarketplaceKey,
                externalCategoryId,
                mapping.ExternalAttributeId,
                cancellationToken);
        }

        CatalogAttributeSnapshot? catalogAttribute = null;
        CatalogVariantSnapshot? catalogVariant = null;

        if (mapping.SourceType == AttributeMappingSourceType.CatalogAttribute)
        {
            catalogAttribute = await catalogAttributes.GetByIdAsync(mapping.CatalogSourceId, cancellationToken);
        }
        else
        {
            catalogVariant = await catalogVariants.GetByIdAsync(mapping.CatalogSourceId, cancellationToken);
        }

        return mapping.ToDto(catalogAttribute, catalogVariant, externalAttribute);
    }

    internal static async Task<IReadOnlyList<AttributeChannelMappingDto>> EnrichManyAsync(
        IReadOnlyList<AttributeChannelMapping> mappings,
        ICategoryChannelMappingRepository categoryMappings,
        IExternalCategoryAttributeRepository externalAttributes,
        ICatalogAttributeGateway catalogAttributes,
        ICatalogVariantGateway catalogVariants,
        CancellationToken cancellationToken)
    {
        var results = new List<AttributeChannelMappingDto>(mappings.Count);

        foreach (var mapping in mappings)
        {
            results.Add(await EnrichAsync(
                mapping,
                categoryMappings,
                externalAttributes,
                catalogAttributes,
                catalogVariants,
                cancellationToken));
        }

        return results;
    }

    internal static async Task<AttributeValueChannelMappingDto> EnrichValueAsync(
        AttributeValueChannelMapping mapping,
        AttributeChannelMapping parentMapping,
        ICategoryChannelMappingRepository categoryMappings,
        IExternalAttributeValueRepository externalValues,
        ICatalogAttributeGateway catalogAttributes,
        ICatalogVariantGateway catalogVariants,
        CancellationToken cancellationToken)
    {
        string? catalogValueName = null;
        ExternalAttributeValue? externalValue = null;

        if (parentMapping.SourceType == AttributeMappingSourceType.CatalogAttribute)
        {
            var catalogValue = await catalogAttributes.GetValueByIdAsync(
                parentMapping.CatalogSourceId,
                mapping.CatalogValueId,
                cancellationToken);

            catalogValueName = catalogValue?.Name;
        }
        else
        {
            var catalogValue = await catalogVariants.GetValueByIdAsync(
                parentMapping.CatalogSourceId,
                mapping.CatalogValueId,
                cancellationToken);

            catalogValueName = catalogValue?.Label;
        }

        var externalCategoryId = await categoryMappings.ResolveExternalIdAsync(
            parentMapping.MarketplaceKey,
            parentMapping.CatalogCategoryId,
            cancellationToken);

        if (externalCategoryId is not null)
        {
            externalValue = await externalValues.GetAsync(
                parentMapping.MarketplaceKey,
                externalCategoryId,
                parentMapping.ExternalAttributeId,
                mapping.ExternalValueId,
                cancellationToken);
        }

        return mapping.ToDto(catalogValueName, externalValue);
    }

    internal static async Task<IReadOnlyList<AttributeValueChannelMappingDto>> EnrichValuesAsync(
        IReadOnlyList<AttributeValueChannelMapping> mappings,
        AttributeChannelMapping parentMapping,
        ICategoryChannelMappingRepository categoryMappings,
        IExternalAttributeValueRepository externalValues,
        ICatalogAttributeGateway catalogAttributes,
        ICatalogVariantGateway catalogVariants,
        CancellationToken cancellationToken)
    {
        var results = new List<AttributeValueChannelMappingDto>(mappings.Count);

        foreach (var mapping in mappings)
        {
            results.Add(await EnrichValueAsync(
                mapping,
                parentMapping,
                categoryMappings,
                externalValues,
                catalogAttributes,
                catalogVariants,
                cancellationToken));
        }

        return results;
    }
}
