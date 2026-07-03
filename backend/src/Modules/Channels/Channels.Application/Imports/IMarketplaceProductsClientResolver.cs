using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Application.Imports;

/// <summary>Pazaryerine göre ürün import client'ı çözümler.</summary>
public interface IMarketplaceProductsClientResolver
{
    /// <summary>Verilen pazaryeri için ürün client'ı döndürür.</summary>
    Result<IMarketplaceProductsClient> Resolve(Marketplace marketplace);
}
