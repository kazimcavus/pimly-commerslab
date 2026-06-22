using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Categories.CreateCategory;

/// <summary>Yeni kategori oluşturma işlemini tanımlayan handler arayüzü.</summary>
public interface ICreateCategoryHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<CategoryDto>> ExecuteAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default);
}
