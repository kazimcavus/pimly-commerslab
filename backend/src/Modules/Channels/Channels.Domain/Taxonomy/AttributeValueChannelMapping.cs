using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Domain.Taxonomy;

/// <summary>AttributeChannelMapping altında catalog değeri ile harici değer eşlemesi.</summary>
public sealed class AttributeValueChannelMapping : AggregateRoot<Guid>
{
    private AttributeValueChannelMapping()
    {
        ExternalValueId = string.Empty;
    }

    private AttributeValueChannelMapping(
        Guid id,
        Guid tenantId,
        Guid attributeChannelMappingId,
        Guid catalogValueId,
        string externalValueId)
        : base(id)
    {
        TenantId = tenantId;
        AttributeChannelMappingId = attributeChannelMappingId;
        CatalogValueId = catalogValueId;
        ExternalValueId = externalValueId;
    }

    public Guid TenantId { get; private set; }

    public Guid AttributeChannelMappingId { get; private set; }

    public Guid CatalogValueId { get; private set; }

    public string ExternalValueId { get; private set; }

    public static Result<AttributeValueChannelMapping> Create(
        Guid tenantId,
        Guid attributeChannelMappingId,
        Guid catalogValueId,
        string externalValueId)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<AttributeValueChannelMapping>(Error.Validation("Tenant id is required."));
        }

        if (attributeChannelMappingId == Guid.Empty)
        {
            return Result.Failure<AttributeValueChannelMapping>(Error.Validation("Attribute channel mapping id is required."));
        }

        if (catalogValueId == Guid.Empty)
        {
            return Result.Failure<AttributeValueChannelMapping>(Error.Validation("Catalog value id is required."));
        }

        if (string.IsNullOrWhiteSpace(externalValueId))
        {
            return Result.Failure<AttributeValueChannelMapping>(Error.Validation("External value id is required."));
        }

        return Result.Success(new AttributeValueChannelMapping(
            Guid.NewGuid(),
            tenantId,
            attributeChannelMappingId,
            catalogValueId,
            externalValueId.Trim()));
    }

    public Result UpdateExternalValue(string externalValueId)
    {
        if (string.IsNullOrWhiteSpace(externalValueId))
        {
            return Result.Failure(Error.Validation("External value id is required."));
        }

        ExternalValueId = externalValueId.Trim();
        return Result.Success();
    }
}
