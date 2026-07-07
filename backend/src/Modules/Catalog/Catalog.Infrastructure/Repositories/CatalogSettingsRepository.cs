using Catalog.Domain.Settings;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

/// <summary>Katalog ayarları için veritabanı erişim katmanı.</summary>
internal sealed class CatalogSettingsRepository(CatalogDbContext db) : ICatalogSettingsRepository
{
    public async Task<CatalogSettings?> GetAsync(CancellationToken cancellationToken = default) =>
        await db.CatalogSettings.FirstOrDefaultAsync(
            s => s.Id == CatalogSettings.SingletonId,
            cancellationToken);

    public async Task AddAsync(CatalogSettings settings, CancellationToken cancellationToken = default) =>
        await db.CatalogSettings.AddAsync(settings, cancellationToken);

    public void Update(CatalogSettings settings) => RepositoryExtensions.UpdateIfDetached(db, settings);
}
