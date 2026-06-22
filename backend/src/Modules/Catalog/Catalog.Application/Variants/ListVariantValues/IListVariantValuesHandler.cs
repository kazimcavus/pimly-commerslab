using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Variants.ListVariantValues;

/// <summary>Varyant değeri listeleme işlemini tanımlayan sözleşme.</summary>
public interface IListVariantValuesHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<PagedResult<VariantValueDto>>> ExecuteAsync(
        ListVariantValuesQuery query,
        CancellationToken cancellationToken = default);
}
