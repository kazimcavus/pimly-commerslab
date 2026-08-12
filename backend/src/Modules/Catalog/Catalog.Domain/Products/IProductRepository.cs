using Catalog.Domain.Products;
using SharedKernel;

namespace Catalog.Domain;

/// <summary>Product aggregate kalıcılık işlemlerini tanımlayan depo arabirimi.</summary>
/// <example>ModelCodeExistsAsync("GOMlek-001") ile benzersizlik kontrolü.</example>
public interface IProductRepository
{
    /// <summary>Belirtilen tanımlayıcıya sahip ürünü kalemleri, eksenleri ve görselleriyle getirir.</summary>
    /// <param name="id">Ürün tanımlayıcısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Ürünleri sayfalanmış olarak listeler.</summary>
    /// <param name="pagination">Sayfalama parametreleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<PagedResult<Product>> ListAsync(Pagination pagination, CancellationToken cancellationToken = default);

    /// <summary>Model koduna göre ürünü getirir.</summary>
    /// <param name="modelCode">Aranacak model kodu.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Product?> GetByModelCodeAsync(string modelCode, CancellationToken cancellationToken = default);

    /// <summary>Belirtilen tanımlayıcıya sahip satılabilir kalemi getirir.</summary>
    /// <param name="itemId">Kalem tanımlayıcısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<ProductItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>Kalem tanımlayıcısı üzerinden üst ürünü getirir.</summary>
    /// <param name="itemId">Kalem tanımlayıcısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Product?> GetByItemIdAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>Görsel tanımlayıcısı üzerinden üst ürünü getirir.</summary>
    /// <param name="imageId">Görsel tanımlayıcısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Product?> GetByImageIdAsync(Guid imageId, CancellationToken cancellationToken = default);

    /// <summary>Model kodunun daha önce kullanılıp kullanılmadığını döner.</summary>
    /// <param name="modelCode">Sorgulanacak model kodu.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<bool> ModelCodeExistsAsync(string modelCode, CancellationToken cancellationToken = default);

    /// <summary>Barkodun herhangi bir ürün kalemine atanmış olup olmadığını döner.</summary>
    /// <param name="barcode">Sorgulanacak barkod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<bool> BarcodeExistsAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>Varyant SKU'sunun daha önce kullanılıp kullanılmadığını döner.</summary>
    /// <param name="sku">Sorgulanacak SKU.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<bool> VariantSkuExistsAsync(string sku, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen barkodlara karşılık gelen ürün kalemi kimliklerini toplu çözer; bulunamayan barkodlar
    /// sonuçta yer almaz.
    /// </summary>
    /// <param name="barcodes">Çözümlenecek barkodlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<IReadOnlyDictionary<string, Guid>> ResolveItemIdsByBarcodeAsync(
        IReadOnlyCollection<string> barcodes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen kalemleri barındıran ürünleri kalem ve görselleriyle birlikte toplu getirir
    /// (pazaryerine gönderilecek içeriğin okunması için).
    /// </summary>
    /// <param name="productItemIds">Kapsanacak kalem kimlikleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<IReadOnlyList<Product>> ListByItemIdsAsync(
        IReadOnlyCollection<Guid> productItemIds,
        CancellationToken cancellationToken = default);

    /// <summary>Verilen kategorilerdeki tüm satılabilir kalemlerin kimliklerini listeler.</summary>
    /// <param name="categoryIds">Kapsanacak kategori kimlikleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<IReadOnlyList<Guid>> ListItemIdsByCategoriesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken = default);

    /// <summary>Yeni ürünü kalıcı depoya ekler.</summary>
    /// <param name="product">Eklenecek ürün.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Var olan ürüne eklenen yeni galeri görselini izlemeye alır. Anahtarı domain'de atanan
    /// yeni child, change tracking keşfinde Modified sayılacağı için açıkça Added işaretlenmelidir.
    /// </summary>
    /// <param name="image">Aggregate'e yeni eklenen görsel.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task AddImageAsync(ProductImage image, CancellationToken cancellationToken = default);

    /// <summary>
    /// Var olan ürüne eklenen yeni satılabilir kalemi izlemeye alır. Anahtarı domain'de atanan
    /// yeni child, change tracking keşfinde Modified sayılacağı için açıkça Added işaretlenmelidir.
    /// </summary>
    /// <param name="item">Aggregate'e yeni eklenen kalem.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task AddItemAsync(ProductItem item, CancellationToken cancellationToken = default);

    /// <summary>Ürün aggregate'indeki değişiklikleri izlemeye alır.</summary>
    /// <param name="product">Güncellenmiş ürün.</param>
    void Update(Product product);

    /// <summary>Ürünü kalıcı depodan siler.</summary>
    /// <param name="product">Silinecek ürün.</param>
    void Remove(Product product);
}
