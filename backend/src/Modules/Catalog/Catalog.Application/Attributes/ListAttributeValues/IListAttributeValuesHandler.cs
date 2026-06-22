using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Attributes.ListAttributeValues;

/// <summary>Özellik değerlerini listeleme işlemini tanımlayan sözleşme.</summary>
public interface IListAttributeValuesHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<PagedResult<AttributeDefinitionValueDto>>> ExecuteAsync(
        ListAttributeValuesQuery query,
        CancellationToken cancellationToken = default);
}
