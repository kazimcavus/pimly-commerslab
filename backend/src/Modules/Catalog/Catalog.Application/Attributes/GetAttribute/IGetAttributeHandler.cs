using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Attributes.GetAttribute;

/// <summary>Öznitelik getirme işlemini tanımlayan sözleşme.</summary>
public interface IGetAttributeHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<AttributeDto>> ExecuteAsync(GetAttributeQuery query, CancellationToken cancellationToken = default);
}
