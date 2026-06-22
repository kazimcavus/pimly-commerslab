using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Categories.ListCategories;

/// <summary>Kategori listeleme işlemini tanımlayan handler arayüzü.</summary>
public interface IListCategoriesHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<PagedResult<CategoryDto>>> ExecuteAsync(
        ListCategoriesQuery query,
        CancellationToken cancellationToken = default);
}
