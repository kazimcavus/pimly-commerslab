namespace Catalog.Domain.Products;

/// <summary>Kalem fiyatları (fiyat tanımı bazlı tutarlar) için veritabanı erişim sözleşmesi.</summary>
public interface IItemPriceRepository
{
    /// <summary>Kalem ve fiyat tanımına göre kalem fiyatını getirir.</summary>
    Task<ProductItemPrice?> GetAsync(
        Guid productItemId,
        Guid priceDefinitionId,
        CancellationToken cancellationToken = default);

    /// <summary>Kalemin tüm fiyatlarını listeler.</summary>
    Task<IReadOnlyList<ProductItemPrice>> ListByItemAsync(
        Guid productItemId,
        CancellationToken cancellationToken = default);

    /// <summary>Yeni kalem fiyatı ekler.</summary>
    Task AddAsync(ProductItemPrice itemPrice, CancellationToken cancellationToken = default);

    /// <summary>Kalem fiyatını günceller.</summary>
    void Update(ProductItemPrice itemPrice);

    /// <summary>Kalem fiyatını siler.</summary>
    void Remove(ProductItemPrice itemPrice);
}
