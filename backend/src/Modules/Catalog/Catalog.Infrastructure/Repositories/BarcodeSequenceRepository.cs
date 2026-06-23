using Catalog.Domain;
using Catalog.Domain.Barcodes;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

/// <summary>Barkod serisi ayarı için veritabanı erişim katmanı.</summary>
internal sealed class BarcodeSequenceRepository(CatalogDbContext db) : IBarcodeSequenceRepository
{
    public async Task<BarcodeSequence?> GetAsync(CancellationToken cancellationToken = default) =>
        await db.BarcodeSequences.FirstOrDefaultAsync(
            s => s.Id == BarcodeSequence.SingletonId,
            cancellationToken);

    public async Task AddAsync(BarcodeSequence sequence, CancellationToken cancellationToken = default) =>
        await db.BarcodeSequences.AddAsync(sequence, cancellationToken);

    public void Update(BarcodeSequence sequence) => RepositoryExtensions.UpdateIfDetached(db, sequence);
}
