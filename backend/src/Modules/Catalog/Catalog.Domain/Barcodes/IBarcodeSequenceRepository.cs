using Catalog.Domain.Barcodes;

namespace Catalog.Domain;

/// <summary>Barkod serisi ayarının kalıcılık işlemlerini tanımlar.</summary>
public interface IBarcodeSequenceRepository
{
    /// <summary>Tenant barkod serisi ayarını getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<BarcodeSequence?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>İlk barkod serisi kaydını ekler.</summary>
    /// <param name="sequence">Kalıcı depoya yazılacak seri ayarı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task AddAsync(BarcodeSequence sequence, CancellationToken cancellationToken = default);

    /// <summary>Barkod serisi ayarındaki değişiklikleri izlemeye alır.</summary>
    /// <param name="sequence">Güncellenmiş seri ayarı.</param>
    void Update(BarcodeSequence sequence);
}
