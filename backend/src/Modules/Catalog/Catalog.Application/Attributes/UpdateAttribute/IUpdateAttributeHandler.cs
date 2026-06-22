using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Attributes.UpdateAttribute;

/// <summary>Öznitelik güncelleme işlemini tanımlayan sözleşme.</summary>
public interface IUpdateAttributeHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<AttributeDto>> ExecuteAsync(UpdateAttributeCommand command, CancellationToken cancellationToken = default);
}
