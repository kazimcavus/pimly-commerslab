using SharedKernel;

namespace Catalog.Application.Variants.DeleteVariantType;

/// <summary>Varyant türü silme işlemini tanımlayan sözleşme.</summary>
public interface IDeleteVariantTypeHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(DeleteVariantTypeCommand command, CancellationToken cancellationToken = default);
}
