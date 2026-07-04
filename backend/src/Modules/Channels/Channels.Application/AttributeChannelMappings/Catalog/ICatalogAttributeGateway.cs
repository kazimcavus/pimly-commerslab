namespace Channels.Application.AttributeChannelMappings.Catalog;

public interface ICatalogAttributeGateway
{
    Task<CatalogAttributeSnapshot?> GetByIdAsync(Guid attributeId, CancellationToken cancellationToken = default);

    Task<bool> AttributeBelongsToCategoryAsync(
        Guid categoryId,
        Guid attributeId,
        CancellationToken cancellationToken = default);

    Task<CatalogAttributeValueSnapshot?> GetValueByIdAsync(
        Guid attributeId,
        Guid valueId,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogAttributeSnapshot(Guid Id, string Key, string Name);

public sealed record CatalogAttributeValueSnapshot(Guid Id, Guid AttributeId, string Name);
