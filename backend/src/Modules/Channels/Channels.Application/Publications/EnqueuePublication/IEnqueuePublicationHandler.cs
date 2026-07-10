using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.Publications.EnqueuePublication;

/// <summary>Ürün yayın job'ı kuyruğa alma işlemini yürüten handler arabirimi.</summary>
public interface IEnqueuePublicationHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ProductPublicationRunDto>> ExecuteAsync(
        EnqueuePublicationCommand command,
        CancellationToken cancellationToken = default);
}
