namespace Channels.Application.ExternalCatalog;

/// <summary>Pazaryerinden çekilen kategori attribute düğümü.</summary>
/// <remarks>IsSlicer: pazaryerinde ayrı ürün kartı oluşturan özellik (ör. Trendyol slicer=Renk).</remarks>
public sealed record MarketplaceCategoryAttributeNode(
    string ExternalAttributeId,
    string Name,
    bool Required,
    bool AllowCustom,
    bool IsVariant,
    IReadOnlyList<MarketplaceAttributeValueNode> Values,
    bool IsSlicer = false);
