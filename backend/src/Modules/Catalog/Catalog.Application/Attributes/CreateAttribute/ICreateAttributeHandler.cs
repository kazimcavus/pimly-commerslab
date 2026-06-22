using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Attributes.CreateAttribute;

/// <summary>Öznitelik oluşturma işlemini tanımlayan sözleşme.</summary>
public interface ICreateAttributeHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<AttributeDto>> ExecuteAsync(CreateAttributeCommand command, CancellationToken cancellationToken = default);
}
