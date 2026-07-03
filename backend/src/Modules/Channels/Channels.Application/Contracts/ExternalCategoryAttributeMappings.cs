using Channels.Application.Contracts;
using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;

namespace Channels.Application.Contracts;

internal static class ExternalCategoryAttributeMappings
{
    internal static ExternalCategoryAttributeDto ToDto(
        this ExternalCategoryAttribute attribute,
        IReadOnlyList<ExternalAttributeValue> values) =>
        new(
            attribute.ExternalCategoryId,
            attribute.ExternalAttributeId,
            attribute.Name,
            attribute.Required,
            attribute.AllowCustom,
            attribute.IsVariant,
            attribute.SyncedAt,
            values
                .Where(value => value.ExternalAttributeId == attribute.ExternalAttributeId)
                .Select(value => value.ToDto())
                .ToList());

    internal static ExternalAttributeValueDto ToDto(this ExternalAttributeValue value) =>
        new(
            value.ExternalAttributeId,
            value.ExternalValueId,
            value.Name,
            value.SyncedAt);
}
