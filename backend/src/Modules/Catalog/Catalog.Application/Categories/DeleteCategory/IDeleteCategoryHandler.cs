using SharedKernel;

namespace Catalog.Application.Categories.DeleteCategory;

/// <summary>Kategori silme işlemini tanımlayan handler arayüzü.</summary>
public interface IDeleteCategoryHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(DeleteCategoryCommand command, CancellationToken cancellationToken = default);
}
