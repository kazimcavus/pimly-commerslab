using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Variants.GetVariantType;

/// <summary>Varyant türü getirme işlemini tanımlayan sözleşme.</summary>
public interface IGetVariantTypeHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<VariantTypeDto>> ExecuteAsync(GetVariantTypeQuery query, CancellationToken cancellationToken = default);
}
