using Pricing.Application.Contracts;
using SharedKernel;

namespace Pricing.Application.PriceDefinitions.UpdatePriceDefinition;

/// <summary>Fiyat tanımı güncelleme işlemini tanımlayan handler arayüzü.</summary>
public interface IUpdatePriceDefinitionHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<PriceDefinitionDto>> ExecuteAsync(
        UpdatePriceDefinitionCommand command,
        CancellationToken cancellationToken = default);
}
