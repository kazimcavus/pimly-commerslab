namespace Channels.Application.Taxonomy;

/// <summary>Pazaryerinden çekilen kategori attribute düğümü.</summary>
public sealed record MarketplaceCategoryAttributeNode(
    string ExternalAttributeId,
    string Name,
    bool Required,
    bool AllowCustom,
    bool IsVariant,
    IReadOnlyList<MarketplaceAttributeValueNode> Values);
