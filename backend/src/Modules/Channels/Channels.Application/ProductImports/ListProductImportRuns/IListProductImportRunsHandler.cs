using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.ProductImports.ListProductImportRuns;

/// <summary>Ürün import run'larını listeleme işlemini yürüten handler arabirimi.</summary>
public interface IListProductImportRunsHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<IReadOnlyList<ProductImportRunSummaryDto>>> ExecuteAsync(
        ListProductImportRunsQuery query,
        CancellationToken cancellationToken = default);
}
