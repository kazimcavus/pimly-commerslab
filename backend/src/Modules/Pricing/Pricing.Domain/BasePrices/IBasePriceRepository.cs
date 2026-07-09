namespace Pricing.Domain.BasePrices;

/// <summary>Kalem temel fiyatları için veritabanı erişim sözleşmesi.</summary>
public interface IBasePriceRepository
{
    /// <summary>Kaleme ait temel fiyatı getirir; yoksa null.</summary>
    Task<BasePrice?> GetByItemAsync(Guid productItemId, CancellationToken cancellationToken = default);

    /// <summary>Yeni temel fiyat ekler.</summary>
    Task AddAsync(BasePrice basePrice, CancellationToken cancellationToken = default);

    /// <summary>Temel fiyatı günceller.</summary>
    void Update(BasePrice basePrice);

    /// <summary>Temel fiyatı siler.</summary>
    void Remove(BasePrice basePrice);
}
