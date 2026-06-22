using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Categories.UpdateCategoryAttribute;

/// <summary>Kategori-özellik atamasını güncelleme işlemini tanımlayan handler arayüzü.</summary>
public interface IUpdateCategoryAttributeHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<CategoryAttributeDto>> ExecuteAsync(
        UpdateCategoryAttributeCommand command,
        CancellationToken cancellationToken = default);
}
