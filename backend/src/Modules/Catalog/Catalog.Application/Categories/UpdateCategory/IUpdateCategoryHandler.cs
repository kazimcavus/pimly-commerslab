using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Categories.UpdateCategory;

/// <summary>Kategori güncelleme işlemini tanımlayan handler arayüzü.</summary>
public interface IUpdateCategoryHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<CategoryDto>> ExecuteAsync(UpdateCategoryCommand command, CancellationToken cancellationToken = default);
}
