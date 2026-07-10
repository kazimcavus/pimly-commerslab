using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.Publications.GetPublicationRun;

/// <summary>Ürün yayın run ayrıntısı getirme işlemini yürüten handler arabirimi.</summary>
public interface IGetPublicationRunHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ProductPublicationRunDto>> ExecuteAsync(
        GetPublicationRunQuery query,
        CancellationToken cancellationToken = default);
}
