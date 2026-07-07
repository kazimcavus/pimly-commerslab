using Catalog.Domain;
using Catalog.Domain.Brands;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Catalog.Infrastructure.Repositories;

/// <summary>Marka varlıkları için veritabanı erişim katmanı.</summary>
internal sealed class BrandRepository(CatalogDbContext db) : IBrandRepository
{
    public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<Brand?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        return await db.Brands
            .FirstOrDefaultAsync(b => EF.Functions.ILike(b.Name, trimmed), cancellationToken);
    }

    public async Task<IReadOnlyList<Brand>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Brands
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Brand>> ListAsync(
        Pagination pagination,
        CancellationToken cancellationToken = default) =>
        await db.Brands
            .OrderBy(b => b.Name)
            .ToPagedResultAsync(pagination, cancellationToken);

    public async Task AddAsync(Brand brand, CancellationToken cancellationToken = default) =>
        await db.Brands.AddAsync(brand, cancellationToken);

    public void Update(Brand brand) => RepositoryExtensions.UpdateIfDetached(db, brand);

    public void Remove(Brand brand) => db.Brands.Remove(brand);
}
