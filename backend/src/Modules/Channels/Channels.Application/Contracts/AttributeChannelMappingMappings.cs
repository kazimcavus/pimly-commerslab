using Channels.Application.Contracts;
using Channels.Application.Ports;
using Channels.Application.Taxonomy;
using Channels.Domain.Taxonomy;

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
            attribute.IsVariant);

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
            mapping.MarketplaceKey.Value,
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
