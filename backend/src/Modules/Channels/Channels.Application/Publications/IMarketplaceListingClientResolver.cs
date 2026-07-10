using SharedKernel;

namespace Channels.Application.Publications;

/// <summary>Pazaryerine göre listeleme (publish) istemcisi çözümler.</summary>
public interface IMarketplaceListingClientResolver
{
    /// <summary>Verilen pazaryeri için listeleme istemcisi döndürür.</summary>
    Result<IMarketplaceListingClient> Resolve(Marketplace marketplace);
}
