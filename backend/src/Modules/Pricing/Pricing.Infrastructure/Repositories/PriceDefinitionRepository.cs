using Microsoft.EntityFrameworkCore;
using Pricing.Domain.PriceDefinitions;
using Pricing.Infrastructure.Persistence;
using SharedKernel;

namespace Pricing.Infrastructure.Repositories;

/// <summary>Fiyat tanımı varlıkları için veritabanı erişim katmanı.</summary>
internal sealed class PriceDefinitionRepository(PricingDbContext db) : IPriceDefinitionRepository
{
    public async Task<PriceDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.PriceDefinitions.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<PriceDefinition?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        return await db.PriceDefinitions
            .FirstOrDefaultAsync(d => EF.Functions.ILike(d.Name, trimmed), cancellationToken);
    }

    public async Task<IReadOnlyList<PriceDefinition>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.PriceDefinitions
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<PriceDefinition>> ListAsync(
        Pagination pagination,
        CancellationToken cancellationToken = default) =>
        await db.PriceDefinitions
            .OrderBy(d => d.Name)
            .ToPagedResultAsync(pagination, cancellationToken);

    public async Task AddAsync(PriceDefinition definition, CancellationToken cancellationToken = default) =>
        await db.PriceDefinitions.AddAsync(definition, cancellationToken);

    public void Update(PriceDefinition definition) => RepositoryExtensions.UpdateIfDetached(db, definition);

    public void Remove(PriceDefinition definition) => db.PriceDefinitions.Remove(definition);
}
