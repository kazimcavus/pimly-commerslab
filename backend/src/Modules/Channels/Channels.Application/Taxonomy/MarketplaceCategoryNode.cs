namespace Channels.Application.Taxonomy;

/// <summary>Pazaryerinden çekilen kategori düğümü.</summary>
public sealed record MarketplaceCategoryNode(
    string ExternalId,
    string Name,
    string? ParentExternalId,
    string Path,
    bool IsLeaf);
