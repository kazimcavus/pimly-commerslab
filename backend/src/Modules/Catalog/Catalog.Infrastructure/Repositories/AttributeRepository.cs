using Catalog.Domain;
using Catalog.Domain.Attributes;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using DomainAttribute = Catalog.Domain.Attributes.Attribute;

namespace Catalog.Infrastructure.Repositories;

/// <summary>Öznitelik tanımları için veritabanı erişim katmanı.</summary>
internal sealed class AttributeRepository(CatalogDbContext db) : IAttributeRepository
{
    public async Task<DomainAttribute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Attributes
            .Include(a => a.Values)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<DomainAttribute?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var attributeKey = AttributeKey.FromPersistence(key);
        return await db.Attributes.FirstOrDefaultAsync(a => a.Key == attributeKey, cancellationToken);
    }

    public async Task<IReadOnlyList<DomainAttribute>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Attributes.OrderBy(a => a.Key).ToListAsync(cancellationToken);

    public async Task<PagedResult<DomainAttribute>> ListAsync(
        Pagination pagination,
        CancellationToken cancellationToken = default) =>
        await db.Attributes
            .OrderBy(a => a.Key)
            .ToPagedResultAsync(pagination, cancellationToken);

    public async Task AddAsync(DomainAttribute attribute, CancellationToken cancellationToken = default) =>
        await db.Attributes.AddAsync(attribute, cancellationToken);

    public void Update(DomainAttribute attribute) => RepositoryExtensions.UpdateIfDetached(db, attribute);

    public void Remove(DomainAttribute attribute) => db.Attributes.Remove(attribute);
}
