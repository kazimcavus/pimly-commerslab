using SharedKernel;

namespace Catalog.Application.Variants.RemoveVariantValue;

/// <summary>Varyant değeri silme işlemini tanımlayan sözleşme.</summary>
public interface IRemoveVariantValueHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(RemoveVariantValueCommand command, CancellationToken cancellationToken = default);
}
