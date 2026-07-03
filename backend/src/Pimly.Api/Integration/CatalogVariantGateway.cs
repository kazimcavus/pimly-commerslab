using Catalog.Application.Variants.GetVariantType;
using Catalog.Domain;
using Channels.Application.Ports;

namespace Pimly.Api.Integration;

/// <summary>Catalog modülünden variant okuma gateway implementasyonu.</summary>
internal sealed class CatalogVariantGateway(
    IGetVariantTypeHandler getVariantType,
    IVariantRepository variants) : ICatalogVariantGateway
{
    public async Task<CatalogVariantSnapshot?> GetByIdAsync(
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        var result = await getVariantType.ExecuteAsync(new GetVariantTypeQuery(variantId), cancellationToken);
        if (result.IsFailure)
        {
            return null;
        }

        var variant = result.Value;
        return new CatalogVariantSnapshot(variant.Id, variant.Key, variant.Name);
    }

    public async Task<CatalogVariantValueSnapshot?> GetValueByIdAsync(
        Guid variantId,
        Guid valueId,
        CancellationToken cancellationToken = default)
    {
        var variant = await variants.GetByIdAsync(variantId, cancellationToken);
        if (variant is null)
        {
            return null;
        }

        var value = variant.Values.FirstOrDefault(entry => entry.Id == valueId);
        if (value is null)
        {
            return null;
        }

        return new CatalogVariantValueSnapshot(value.Id, variantId, value.Label);
    }
}
