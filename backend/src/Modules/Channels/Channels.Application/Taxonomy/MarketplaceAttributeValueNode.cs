namespace Channels.Application.Taxonomy;

/// <summary>Pazaryerinden çekilen attribute değeri.</summary>
public sealed record MarketplaceAttributeValueNode(
    string ExternalValueId,
    string Name);
