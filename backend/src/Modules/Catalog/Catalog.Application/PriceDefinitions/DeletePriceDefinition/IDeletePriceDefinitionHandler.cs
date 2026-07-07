using SharedKernel;

namespace Catalog.Application.PriceDefinitions.DeletePriceDefinition;

/// <summary>Fiyat tanımı silme işlemini tanımlayan handler arayüzü.</summary>
public interface IDeletePriceDefinitionHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(DeletePriceDefinitionCommand command, CancellationToken cancellationToken = default);
}
