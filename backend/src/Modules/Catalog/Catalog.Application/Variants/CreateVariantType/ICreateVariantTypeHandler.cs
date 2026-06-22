using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Variants.CreateVariantType;

/// <summary>Varyant türü oluşturma işlemini tanımlayan sözleşme.</summary>
public interface ICreateVariantTypeHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<VariantTypeDto>> ExecuteAsync(CreateVariantTypeCommand command, CancellationToken cancellationToken = default);
}
