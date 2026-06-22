using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.Categories.AssignCategoryAttribute;

/// <summary>Kategoriye özellik atama işlemini tanımlayan handler arayüzü.</summary>
public interface IAssignCategoryAttributeHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="command">Calistirilacak komut.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result<CategoryAttributeDto>> ExecuteAsync(
        AssignCategoryAttributeCommand command,
        CancellationToken cancellationToken = default);
}
