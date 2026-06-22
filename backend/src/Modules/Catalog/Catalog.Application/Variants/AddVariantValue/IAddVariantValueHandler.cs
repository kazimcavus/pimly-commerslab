using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Variants.AddVariantValue;

/// <summary>Varyant değeri ekleme işlemini tanımlayan sözleşme.</summary>
public interface IAddVariantValueHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<VariantValueDto>> ExecuteAsync(AddVariantValueCommand command, CancellationToken cancellationToken = default);
}
