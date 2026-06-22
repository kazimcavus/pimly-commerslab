using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Categories.ListCategoryAttributes;

/// <summary>Kategori özelliklerini listeleme işlemini tanımlayan handler arayüzü.</summary>
public interface IListCategoryAttributesHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<PagedResult<CategoryAttributeDto>>> ExecuteAsync(
        ListCategoryAttributesQuery query,
        CancellationToken cancellationToken = default);
}
