using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.Imports.GetProductImportRun;

/// <summary>Ürün import run ayrıntısını getirme işlemini yürüten handler arabirimi.</summary>
public interface IGetProductImportRunHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<ProductImportRunDto>> ExecuteAsync(
        GetProductImportRunQuery query,
        CancellationToken cancellationToken = default);
}
