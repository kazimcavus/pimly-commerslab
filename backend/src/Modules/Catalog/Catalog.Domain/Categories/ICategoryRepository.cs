using Catalog.Domain.Categories;
using SharedKernel;

namespace Catalog.Domain;

/// <summary>
/// Kategori varlıklarının kalıcılık işlemlerini tanımlayan depo arabirimi.
/// </summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<Category>> ListAsync(Pagination pagination, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> GetDescendantIdsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    void Update(Category category);

    void Remove(Category category);
}
