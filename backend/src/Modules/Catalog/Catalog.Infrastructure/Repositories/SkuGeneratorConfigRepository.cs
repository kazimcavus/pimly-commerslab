using Catalog.Domain.SkuGenerator;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

/// <summary>SKU üretici yapılandırması için veritabanı erişim katmanı.</summary>
internal sealed class SkuGeneratorConfigRepository(CatalogDbContext db) : ISkuGeneratorConfigRepository
{
    public async Task<SkuGeneratorConfig?> GetAsync(CancellationToken cancellationToken = default) =>
        await db.SkuGeneratorConfigs.FirstOrDefaultAsync(
            c => c.Id == SkuGeneratorConfig.SingletonId,
            cancellationToken);

    public async Task AddAsync(SkuGeneratorConfig config, CancellationToken cancellationToken = default) =>
        await db.SkuGeneratorConfigs.AddAsync(config, cancellationToken);

    public void Update(SkuGeneratorConfig config) => RepositoryExtensions.UpdateIfDetached(db, config);
}
