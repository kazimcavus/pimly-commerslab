using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Domain.Taxonomy;

/// <summary>Catalog attribute/variant ile harici attribute arasındaki alan eşlemesi.</summary>
public sealed class AttributeChannelMapping : AggregateRoot<Guid>
{
    private AttributeChannelMapping()
    {
        MarketplaceKey = null!;
        ExternalAttributeId = string.Empty;
    }

    private AttributeChannelMapping(
        Guid id,
        Guid tenantId,
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        AttributeMappingSourceType sourceType,
        Guid catalogSourceId,
        string externalAttributeId)
        : base(id)
    {
        TenantId = tenantId;
        MarketplaceKey = marketplaceKey;
        CatalogCategoryId = catalogCategoryId;
        SourceType = sourceType;
        CatalogSourceId = catalogSourceId;
        ExternalAttributeId = externalAttributeId;
    }

    public Guid TenantId { get; private set; }

    public MarketplaceKey MarketplaceKey { get; private set; }

    public Guid CatalogCategoryId { get; private set; }

    public AttributeMappingSourceType SourceType { get; private set; }

    public Guid CatalogSourceId { get; private set; }

    public string ExternalAttributeId { get; private set; }

    public static Result<AttributeChannelMapping> Create(
        Guid tenantId,
        MarketplaceKey marketplaceKey,
        Guid catalogCategoryId,
        AttributeMappingSourceType sourceType,
        Guid catalogSourceId,
        string externalAttributeId)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<AttributeChannelMapping>(Error.Validation("Tenant id is required."));
        }

        if (catalogCategoryId == Guid.Empty)
        {
            return Result.Failure<AttributeChannelMapping>(Error.Validation("Catalog category id is required."));
        }

        if (catalogSourceId == Guid.Empty)
        {
            return Result.Failure<AttributeChannelMapping>(Error.Validation("Catalog source id is required."));
        }

        if (string.IsNullOrWhiteSpace(externalAttributeId))
        {
            return Result.Failure<AttributeChannelMapping>(Error.Validation("External attribute id is required."));
        }

        return Result.Success(new AttributeChannelMapping(
            Guid.NewGuid(),
            tenantId,
            marketplaceKey,
            catalogCategoryId,
            sourceType,
            catalogSourceId,
            externalAttributeId.Trim()));
    }

    public Result UpdateExternalAttribute(string externalAttributeId)
    {
        if (string.IsNullOrWhiteSpace(externalAttributeId))
        {
            return Result.Failure(Error.Validation("External attribute id is required."));
        }

        ExternalAttributeId = externalAttributeId.Trim();
        return Result.Success();
    }
}
