using Catalog.Domain.Variants;
using SharedKernel;

namespace Catalog.Domain;

/// <summary>
/// Varyant tanımlarının kalıcılık işlemlerini tanımlayan depo arabirimi.
/// </summary>
public interface IVariantRepository
{
    Task<Variant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Variant?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<Variant?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<Variant?> GetSlicerVariantAsync(Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Variant>> ListAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<Variant>> ListAsync(Pagination pagination, CancellationToken cancellationToken = default);

    Task AddAsync(Variant variant, CancellationToken cancellationToken = default);

    void Update(Variant variant);

    void Remove(Variant variant);
}
