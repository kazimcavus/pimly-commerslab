using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.PriceDefinitions.CreatePriceDefinition;

/// <summary>Yeni fiyat tanımı oluşturma işlemini tanımlayan handler arayüzü.</summary>
public interface ICreatePriceDefinitionHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<PriceDefinitionDto>> ExecuteAsync(
        CreatePriceDefinitionCommand command,
        CancellationToken cancellationToken = default);
}
