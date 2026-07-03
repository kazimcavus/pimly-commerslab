using Catalog.Domain.Categories;
using SharedKernel;

namespace Catalog.Domain;

/// <summary>
/// Kategori varlıklarının kalıcılık işlemlerini tanımlayan depo arabirimi.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>Belirtilen tanımlayıcıya sahip kategoriyi atamalarıyla birlikte getirir.</summary>
    /// <param name="id">Kategori tanımlayıcısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Tüm kategorileri ada göre sıralı listeler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Kategorileri ada göre sıralı ve sayfalanmış olarak listeler.</summary>
    /// <param name="pagination">Sayfalama parametreleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<PagedResult<Category>> ListAsync(Pagination pagination, CancellationToken cancellationToken = default);

    /// <summary>Belirtilen kategorinin tüm alt soy tanımlayıcılarını döner.</summary>
    /// <param name="categoryId">Kök alınacak kategori tanımlayıcısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<IReadOnlySet<Guid>> GetDescendantIdsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>Yeni kategoriyi kalıcı depoya ekler.</summary>
    /// <param name="category">Eklenecek kategori.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>Kategori aggregate'indeki değişiklikleri izlemeye alır.</summary>
    /// <param name="category">Güncellenmiş kategori.</param>
    void Update(Category category);

    /// <summary>Kategoriyi kalıcı depodan siler.</summary>
    /// <param name="category">Silinecek kategori.</param>
    void Remove(Category category);
}
