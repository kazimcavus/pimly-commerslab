using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.PriceDefinitions.GetPriceDefinition;

/// <summary>Fiyat tanımı getirme işlemini tanımlayan handler arayüzü.</summary>
public interface IGetPriceDefinitionHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<PriceDefinitionDto>> ExecuteAsync(
        GetPriceDefinitionQuery query,
        CancellationToken cancellationToken = default);
}
