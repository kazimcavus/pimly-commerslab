using SharedKernel;

namespace Catalog.Application.Attributes.DeleteAttribute;

/// <summary>Öznitelik silme işlemini tanımlayan sözleşme.</summary>
public interface IDeleteAttributeHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(DeleteAttributeCommand command, CancellationToken cancellationToken = default);
}
