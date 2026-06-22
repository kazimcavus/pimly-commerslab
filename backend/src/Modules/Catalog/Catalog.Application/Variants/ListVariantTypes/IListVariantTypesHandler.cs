using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Variants.ListVariantTypes;

/// <summary>Varyant türü listeleme işlemini tanımlayan sözleşme.</summary>
public interface IListVariantTypesHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<PagedResult<VariantTypeDto>>> ExecuteAsync(
        ListVariantTypesQuery query,
        CancellationToken cancellationToken = default);
}
