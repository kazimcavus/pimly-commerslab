using Catalog.Domain.Variants;
using SharedKernel;

namespace Catalog.Domain;

/// <summary>
/// Varyant tanımlarının kalıcılık işlemlerini tanımlayan depo arabirimi.
/// </summary>
public interface IVariantRepository
{
    /// <summary>Belirtilen tanımlayıcıya sahip varyant türünü değerleriyle getirir.</summary>
    /// <param name="id">Varyant tanımlayıcısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Variant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Ada göre varyant türünü getirir.</summary>
    /// <param name="name">Aranacak tür adı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Variant?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Anahtara göre varyant türünü getirir.</summary>
    /// <param name="key">Aranacak tür anahtarı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Variant?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Slicer olarak işaretlenmiş varyant türünü getirir; isteğe bağlı olarak bir kaydı hariç tutar.</summary>
    /// <param name="excludeId">Sonuçtan hariç tutulacak varyant tanımlayıcısı; opsiyonel.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Variant?> GetSlicerVariantAsync(Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Tüm varyant türlerini listeler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<IReadOnlyList<Variant>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Varyant türlerini sayfalanmış olarak listeler.</summary>
    /// <param name="pagination">Sayfalama parametreleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<PagedResult<Variant>> ListAsync(Pagination pagination, CancellationToken cancellationToken = default);

    /// <summary>Yeni varyant türünü kalıcı depoya ekler.</summary>
    /// <param name="variant">Eklenecek varyant.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task AddAsync(Variant variant, CancellationToken cancellationToken = default);

    /// <summary>Varyant aggregate'indeki değişiklikleri izlemeye alır.</summary>
    /// <param name="variant">Güncellenmiş varyant.</param>
    void Update(Variant variant);

    /// <summary>Varyant türünü kalıcı depodan siler.</summary>
    /// <param name="variant">Silinecek varyant.</param>
    void Remove(Variant variant);
}
