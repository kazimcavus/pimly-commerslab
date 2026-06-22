using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Variants.UpdateVariantType;

/// <summary>Varyant türü güncelleme işlemini tanımlayan sözleşme.</summary>
public interface IUpdateVariantTypeHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<VariantTypeDto>> ExecuteAsync(UpdateVariantTypeCommand command, CancellationToken cancellationToken = default);
}
