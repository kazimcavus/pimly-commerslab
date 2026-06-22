using Catalog.Domain;

namespace Catalog.Application.Variants;

/// <summary>Varyant değeri kimliğine göre varyant türünü bulan yardımcı sınıf.</summary>
internal static class VariantTypeLookup
{
    internal static async Task<Domain.Variants.Variant?> FindByValueIdAsync(
        IVariantRepository variantTypes,
        Guid valueId,
        CancellationToken cancellationToken)
    {
        foreach (var summary in await variantTypes.ListAsync(cancellationToken))
        {
            var loaded = await variantTypes.GetByIdAsync(summary.Id, cancellationToken);
            if (loaded?.Values.Any(v => v.Id == valueId) == true)
            {
                return loaded;
            }
        }

        return null;
    }
}
