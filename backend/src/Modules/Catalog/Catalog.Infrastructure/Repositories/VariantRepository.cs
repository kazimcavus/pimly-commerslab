using Catalog.Domain;
using Catalog.Domain.Variants;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Catalog.Infrastructure.Repositories;

/// <summary>Varyant tanımları için veritabanı erişim katmanı.</summary>
internal sealed class VariantRepository(CatalogDbContext db) : IVariantRepository
{
    public async Task<Variant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Variants
            .Include(v => v.Values)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<Variant?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        await db.Variants
            .FirstOrDefaultAsync(v => v.Name == name, cancellationToken);

    public async Task<Variant?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var variantKey = VariantKey.FromPersistence(key);
        return await db.Variants.FirstOrDefaultAsync(v => v.Key == variantKey, cancellationToken);
    }

    public async Task<Variant?> GetSlicerVariantAsync(
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Variants.Where(v => v.Slicer);
        if (excludeId.HasValue)
        {
            query = query.Where(v => v.Id != excludeId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Variant>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Variants
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Name)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Variant>> ListAsync(
        Pagination pagination,
        CancellationToken cancellationToken = default) =>
        await db.Variants
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Name)
            .ToPagedResultAsync(pagination, cancellationToken);

    public async Task AddAsync(Variant variant, CancellationToken cancellationToken = default) =>
        await db.Variants.AddAsync(variant, cancellationToken);

    public void Update(Variant variant) => RepositoryExtensions.UpdateIfDetached(db, variant);

    public void Remove(Variant variant) => db.Variants.Remove(variant);
}
