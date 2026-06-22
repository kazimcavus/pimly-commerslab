using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Attributes.ListAttributes;

/// <summary>Öznitelik listeleme işlemini tanımlayan sözleşme.</summary>
public interface IListAttributesHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<PagedResult<AttributeDto>>> ExecuteAsync(
        ListAttributesQuery query,
        CancellationToken cancellationToken = default);
}
