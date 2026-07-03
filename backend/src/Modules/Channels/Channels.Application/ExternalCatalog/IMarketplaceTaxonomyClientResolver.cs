using Channels.Domain.Marketplaces;
using SharedKernel;

namespace Channels.Application.ExternalCatalog;

/// <summary>Pazaryerine göre taxonomy client çözümler.</summary>
public interface IMarketplaceTaxonomyClientResolver
{
    /// <summary>Verilen pazaryeri için taxonomy client döndürür.</summary>
    Result<IMarketplaceTaxonomyClient> Resolve(Marketplace marketplace);
}
