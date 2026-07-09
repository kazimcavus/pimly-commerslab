using Pricing.Application.Contracts;
using SharedKernel;

namespace Pricing.Application.PriceDefinitions.ListPriceDefinitions;

/// <summary>Fiyat tanımı listeleme işlemini tanımlayan handler arayüzü.</summary>
public interface IListPriceDefinitionsHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<PagedResult<PriceDefinitionDto>>> ExecuteAsync(
        ListPriceDefinitionsQuery query,
        CancellationToken cancellationToken = default);
}
