using SharedKernel;

namespace Channels.Application.Listings.ContentSync;

/// <summary>Bir pazaryerindeki kirli içeriklerin senkronunu yürüten handler arabirimi.</summary>
public interface ISyncListingContentHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="marketplaceCode">Senkronlanacak pazaryeri kodu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Senkron turunun özeti.</returns>
    Task<Result<ContentSyncSummary>> ExecuteAsync(
        string marketplaceCode,
        CancellationToken cancellationToken = default);
}

/// <summary>Bir içerik senkron turunun sonucu.</summary>
/// <param name="Examined">İncelenen kirli listeleme sayısı.</param>
/// <param name="Skipped">Hash aynı veya ön koşul eksik olduğu için atlanan sayısı.</param>
/// <param name="Created">Pazaryerinde yeni kart olarak gönderilen sayısı.</param>
/// <param name="Updated">Mevcut kartı güncellenen sayısı.</param>
/// <param name="Failed">Gönderimi başarısız olan sayısı.</param>
public sealed record ContentSyncSummary(int Examined, int Skipped, int Created, int Updated, int Failed);
