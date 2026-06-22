using Catalog.Domain;

namespace Catalog.Application.Attributes;

/// <summary>Özellik değeri kimliğine göre özelliği bulan yardımcı sınıf.</summary>
internal static class AttributeLookup
{
    internal static async Task<Domain.Attributes.Attribute?> FindByValueIdAsync(
        IAttributeRepository attributes,
        Guid valueId,
        CancellationToken cancellationToken)
    {
        foreach (var summary in await attributes.ListAsync(cancellationToken))
        {
            var loaded = await attributes.GetByIdAsync(summary.Id, cancellationToken);
            if (loaded?.Values.Any(v => v.Id == valueId) == true)
            {
                return loaded;
            }
        }

        return null;
    }
}
