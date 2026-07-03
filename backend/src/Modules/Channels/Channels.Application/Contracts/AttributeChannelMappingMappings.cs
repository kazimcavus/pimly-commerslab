using Channels.Application.AttributeChannelMappings;
using Channels.Application.Contracts;
using Channels.Application.ExternalCatalog;
using Channels.Application.Ports;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;

namespace Channels.Application.Contracts;

internal static class AttributeChannelMappingMappings
{
    internal static CatalogAttributeSnapshotDto ToDto(this CatalogAttributeSnapshot snapshot) =>
        new(snapshot.Id, snapshot.Key, snapshot.Name);

    internal static CatalogVariantSnapshotDto ToDto(this CatalogVariantSnapshot snapshot) =>
        new(snapshot.Id, snapshot.Key, snapshot.Name);

    internal static ExternalCategoryAttributeSummaryDto ToSummaryDto(this ExternalCategoryAttribute attribute) =>
        new(
            attribute.ExternalAttributeId,
            attribute.Name,
            attribute.Required,
            attribute.AllowCustom,
            attribute.IsVariant,
            attribute.IsSlicer);

    internal static ExternalAttributeValueSummaryDto ToSummaryDto(this ExternalAttributeValue value) =>
        new(value.ExternalValueId, value.Name);

    internal static AttributeChannelMappingDto ToDto(
        this AttributeChannelMapping mapping,
        CatalogAttributeSnapshot? catalogAttribute,
        CatalogVariantSnapshot? catalogVariant,
        ExternalCategoryAttribute? externalAttribute) =>
        new(
            mapping.Id,
            mapping.CatalogCategoryId,
            mapping.Marketplace.Code,
            AttributeMappingSourceTypeParser.ToApiValue(mapping.SourceType),
            mapping.CatalogSourceId,
            mapping.ExternalAttributeId,
            catalogAttribute?.ToDto(),
            catalogVariant?.ToDto(),
            externalAttribute?.ToSummaryDto());

    internal static AttributeValueChannelMappingDto ToDto(
        this AttributeValueChannelMapping mapping,
        string? catalogValueName,
        ExternalAttributeValue? externalValue) =>
        new(
            mapping.Id,
            mapping.AttributeChannelMappingId,
            mapping.CatalogValueId,
            mapping.ExternalValueId,
            catalogValueName,
            externalValue?.ToSummaryDto());
}
