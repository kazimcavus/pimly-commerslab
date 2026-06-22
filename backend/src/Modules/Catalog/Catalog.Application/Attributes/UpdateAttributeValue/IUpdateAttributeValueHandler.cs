using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Attributes.UpdateAttributeValue;

/// <summary>Özellik değeri güncelleme işlemini tanımlayan sözleşme.</summary>
public interface IUpdateAttributeValueHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<AttributeDefinitionValueDto>> ExecuteAsync(UpdateAttributeValueCommand command, CancellationToken cancellationToken = default);
}
