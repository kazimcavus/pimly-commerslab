using Catalog.Domain.Barcodes;

namespace Catalog.Domain;

/// <summary>Barkod serisi ayarının kalıcılık işlemlerini tanımlar.</summary>
public interface IBarcodeSequenceRepository
{
    Task<BarcodeSequence?> GetAsync(CancellationToken cancellationToken = default);

    Task AddAsync(BarcodeSequence sequence, CancellationToken cancellationToken = default);

    void Update(BarcodeSequence sequence);
}
