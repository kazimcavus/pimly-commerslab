using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Variants.UpdateVariantValue;

/// <summary>Varyant değeri güncelleme işlemini tanımlayan sözleşme.</summary>
public interface IUpdateVariantValueHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<VariantValueDto>> ExecuteAsync(UpdateVariantValueCommand command, CancellationToken cancellationToken = default);
}
