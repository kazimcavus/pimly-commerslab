using SharedKernel;

namespace Catalog.Application.Categories.RemoveCategoryAttribute;

/// <summary>Kategori-özellik atamasını kaldırma işlemini tanımlayan handler arayüzü.</summary>
public interface IRemoveCategoryAttributeHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(RemoveCategoryAttributeCommand command, CancellationToken cancellationToken = default);
}
