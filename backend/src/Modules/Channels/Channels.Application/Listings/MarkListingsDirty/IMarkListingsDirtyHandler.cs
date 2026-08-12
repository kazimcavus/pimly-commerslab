using SharedKernel;

namespace Channels.Application.Listings.MarkListingsDirty;

/// <summary>Listeleme kirlilik işaretlemesini yürüten handler arabirimi.</summary>
public interface IMarkListingsDirtyHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>İşaretlenen listeleme sayısı.</returns>
    Task<Result<int>> ExecuteAsync(
        MarkListingsDirtyCommand command,
        CancellationToken cancellationToken = default);
}
