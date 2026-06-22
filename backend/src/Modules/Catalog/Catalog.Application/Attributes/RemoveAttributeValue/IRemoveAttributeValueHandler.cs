using SharedKernel;

namespace Catalog.Application.Attributes.RemoveAttributeValue;

/// <summary>Özellik değeri silme işlemini tanımlayan sözleşme.</summary>
public interface IRemoveAttributeValueHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(RemoveAttributeValueCommand command, CancellationToken cancellationToken = default);
}
