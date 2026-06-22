using SharedKernel;

namespace Catalog.Domain;

/// <summary>
/// Öznitelik tanımlarının kalıcılık işlemlerini tanımlayan depo arabirimi.
/// </summary>
public interface IAttributeRepository
{
    Task<Attributes.Attribute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Attributes.Attribute?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Attributes.Attribute>> ListAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<Attributes.Attribute>> ListAsync(Pagination pagination, CancellationToken cancellationToken = default);

    Task AddAsync(Attributes.Attribute attribute, CancellationToken cancellationToken = default);

    void Update(Attributes.Attribute attribute);

    void Remove(Attributes.Attribute attribute);
}
