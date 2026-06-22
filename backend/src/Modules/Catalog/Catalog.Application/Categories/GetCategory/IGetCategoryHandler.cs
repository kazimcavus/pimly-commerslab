using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Categories.GetCategory;

/// <summary>Kategori getirme işlemini tanımlayan handler arayüzü.</summary>
public interface IGetCategoryHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="query">Calistirilacak sorgu.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<CategoryDto>> ExecuteAsync(GetCategoryQuery query, CancellationToken cancellationToken = default);
}
