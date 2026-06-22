using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Attributes.AddAttributeValue;

/// <summary>Özellik değeri ekleme işlemini tanımlayan sözleşme.</summary>
public interface IAddAttributeValueHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<AttributeDefinitionValueDto>> ExecuteAsync(AddAttributeValueCommand command, CancellationToken cancellationToken = default);
}
