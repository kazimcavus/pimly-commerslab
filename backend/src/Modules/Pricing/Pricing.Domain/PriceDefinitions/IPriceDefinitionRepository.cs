using SharedKernel;

namespace Pricing.Domain.PriceDefinitions;

/// <summary>
/// Fiyat tanımı varlıklarının kalıcılık işlemlerini tanımlayan depo arabirimi.
/// </summary>
public interface IPriceDefinitionRepository
{
    /// <summary>Belirtilen tanımlayıcıya sahip fiyat tanımını getirir.</summary>
    /// <param name="id">Fiyat tanımı tanımlayıcısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<PriceDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Fiyat tanımını ada göre (tenant içinde, büyük/küçük harf duyarsız) getirir; import'ta idempotent garanti için kullanılır.</summary>
    /// <param name="name">Aranacak fiyat tanımı adı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<PriceDefinition?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Tüm fiyat tanımlarını ada göre sıralı listeler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<IReadOnlyList<PriceDefinition>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Fiyat tanımlarını ada göre sıralı ve sayfalanmış olarak listeler.</summary>
    /// <param name="pagination">Sayfalama parametreleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<PagedResult<PriceDefinition>> ListAsync(Pagination pagination, CancellationToken cancellationToken = default);

    /// <summary>Yeni fiyat tanımını kalıcı depoya ekler.</summary>
    /// <param name="definition">Eklenecek fiyat tanımı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task AddAsync(PriceDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>Fiyat tanımı aggregate'indeki değişiklikleri izlemeye alır.</summary>
    /// <param name="definition">Güncellenmiş fiyat tanımı.</param>
    void Update(PriceDefinition definition);

    /// <summary>Fiyat tanımını kalıcı depodan siler.</summary>
    /// <param name="definition">Silinecek fiyat tanımı.</param>
    void Remove(PriceDefinition definition);
}
