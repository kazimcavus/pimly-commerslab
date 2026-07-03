using Channels.Domain.AttributeChannelMappings;
using Channels.Domain.CategoryChannelMappings;
using Channels.Domain.ExternalCatalog;
using Channels.Domain.TaxonomySync;
using SharedKernel;

namespace Channels.Application.AttributeChannelMappings;

internal static class AttributeMappingSourceTypeParser
{
    internal static Result<AttributeMappingSourceType> Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<AttributeMappingSourceType>(Error.Validation("Source type is required."));
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "catalog_attribute" => Result.Success(AttributeMappingSourceType.CatalogAttribute),
            "catalog_variant" => Result.Success(AttributeMappingSourceType.CatalogVariant),
            _ => Result.Failure<AttributeMappingSourceType>(Error.Validation("Source type must be catalog_attribute or catalog_variant.")),
        };
    }

    internal static string ToApiValue(AttributeMappingSourceType sourceType) =>
        sourceType switch
        {
            AttributeMappingSourceType.CatalogAttribute => "catalog_attribute",
            AttributeMappingSourceType.CatalogVariant => "catalog_variant",
            _ => "catalog_attribute",
        };
}
