using SharedKernel;

namespace Channels.Application.Listings.OfferSync;

/// <summary>Bir pazaryerindeki kirli tekliflerin senkronunu yürüten handler arabirimi.</summary>
public interface ISyncListingOffersHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="marketplaceCode">Senkronlanacak pazaryeri kodu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Senkron turunun özeti.</returns>
    Task<Result<OfferSyncSummary>> ExecuteAsync(
        string marketplaceCode,
        CancellationToken cancellationToken = default);
}

/// <summary>Bir senkron turunun sonucu.</summary>
/// <param name="Examined">İncelenen kirli listeleme sayısı.</param>
/// <param name="Skipped">Hash aynı olduğu için çağrı yapılmadan atlanan sayısı.</param>
/// <param name="Pushed">Pazaryerine başarıyla gönderilen sayısı.</param>
/// <param name="Failed">Gönderimi başarısız olan sayısı.</param>
public sealed record OfferSyncSummary(int Examined, int Skipped, int Pushed, int Failed);
