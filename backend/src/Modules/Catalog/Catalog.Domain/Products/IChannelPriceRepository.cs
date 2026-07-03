namespace Catalog.Domain.Products;

/// <summary>Kanal fiyatları için veritabanı erişim sözleşmesi.</summary>
public interface IChannelPriceRepository
{
    /// <summary>Kalem ve pazaryeri anahtarına göre kanal fiyatını getirir.</summary>
    Task<ProductItemChannelPrice?> GetAsync(
        Guid productItemId,
        string marketplaceKey,
        CancellationToken cancellationToken = default);

    /// <summary>Kalemin tüm kanal fiyatlarını listeler.</summary>
    Task<IReadOnlyList<ProductItemChannelPrice>> ListByItemAsync(
        Guid productItemId,
        CancellationToken cancellationToken = default);

    /// <summary>Ürünün tüm kalemlerine ait kanal fiyatlarını listeler.</summary>
    Task<IReadOnlyList<ProductItemChannelPrice>> ListByProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>Yeni kanal fiyatı ekler.</summary>
    Task AddAsync(ProductItemChannelPrice channelPrice, CancellationToken cancellationToken = default);

    /// <summary>Kanal fiyatını günceller.</summary>
    void Update(ProductItemChannelPrice channelPrice);

    /// <summary>Kanal fiyatını siler.</summary>
    void Remove(ProductItemChannelPrice channelPrice);
}
