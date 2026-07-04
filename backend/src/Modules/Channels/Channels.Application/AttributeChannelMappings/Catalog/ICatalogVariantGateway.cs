namespace Channels.Application.AttributeChannelMappings.Catalog;

public interface ICatalogVariantGateway
{
    Task<CatalogVariantSnapshot?> GetByIdAsync(Guid variantId, CancellationToken cancellationToken = default);

    Task<CatalogVariantValueSnapshot?> GetValueByIdAsync(
        Guid variantId,
        Guid valueId,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogVariantSnapshot(Guid Id, string Key, string Name);

public sealed record CatalogVariantValueSnapshot(Guid Id, Guid VariantId, string Label);
