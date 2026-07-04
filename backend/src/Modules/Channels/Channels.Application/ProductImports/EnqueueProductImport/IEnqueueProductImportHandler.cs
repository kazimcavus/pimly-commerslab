using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.ProductImports.EnqueueProductImport;

/// <summary>Ürün import job'ı kuyruğa alma işlemini yürüten handler arabirimi.</summary>
public interface IEnqueueProductImportHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ProductImportRunDto>> ExecuteAsync(
        EnqueueProductImportCommand command,
        CancellationToken cancellationToken = default);
}
