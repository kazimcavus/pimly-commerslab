using SharedKernel;

namespace Channels.Application.ExternalCatalog;

/// <summary>Pazaryerine göre kategori attribute client çözümler.</summary>
public interface IMarketplaceCategoryAttributesClientResolver
{
    /// <summary>Verilen pazaryeri için kategori attribute client döndürür.</summary>
    Result<IMarketplaceCategoryAttributesClient> Resolve(Marketplace marketplace);
}
