using Catalog.Domain.Brands;
using SharedKernel;

namespace Catalog.Domain;

/// <summary>
/// Marka varlıklarının kalıcılık işlemlerini tanımlayan depo arabirimi.
/// </summary>
public interface IBrandRepository
{
    /// <summary>Belirtilen tanımlayıcıya sahip markayı getirir.</summary>
    /// <param name="id">Marka tanımlayıcısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Markayı ada göre (tenant içinde, büyük/küçük harf duyarsız) getirir; import'ta idempotent garanti için kullanılır.</summary>
    /// <param name="name">Aranacak marka adı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Brand?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Tüm markaları ada göre sıralı listeler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<IReadOnlyList<Brand>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Markaları ada göre sıralı ve sayfalanmış olarak listeler.</summary>
    /// <param name="pagination">Sayfalama parametreleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<PagedResult<Brand>> ListAsync(Pagination pagination, CancellationToken cancellationToken = default);

    /// <summary>Yeni markayı kalıcı depoya ekler.</summary>
    /// <param name="brand">Eklenecek marka.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task AddAsync(Brand brand, CancellationToken cancellationToken = default);

    /// <summary>Marka aggregate'indeki değişiklikleri izlemeye alır.</summary>
    /// <param name="brand">Güncellenmiş marka.</param>
    void Update(Brand brand);

    /// <summary>Markayı kalıcı depodan siler.</summary>
    /// <param name="brand">Silinecek marka.</param>
    void Remove(Brand brand);
}
