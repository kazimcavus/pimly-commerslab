using SharedKernel;

namespace Pricing.Domain.ChannelPrices;

/// <summary>Kalem kanal fiyatları için veritabanı erişim sözleşmesi.</summary>
public interface IChannelPriceRepository
{
    /// <summary>Kalem ve pazaryerine göre kanal fiyatını getirir; yoksa null.</summary>
    Task<ChannelPrice?> GetAsync(
        Guid productItemId,
        Marketplace marketplace,
        CancellationToken cancellationToken = default);

    /// <summary>Kalemin tüm kanal fiyatlarını (pazaryeri bazlı) listeler.</summary>
    Task<IReadOnlyList<ChannelPrice>> ListByItemAsync(
        Guid productItemId,
        CancellationToken cancellationToken = default);

    /// <summary>Bir pazaryerindeki tüm kanal fiyatlarını (tenant kapsamında) listeler; yayın kaynağıdır.</summary>
    Task<IReadOnlyList<ChannelPrice>> ListByMarketplaceAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default);

    /// <summary>Yeni kanal fiyatı ekler.</summary>
    Task AddAsync(ChannelPrice channelPrice, CancellationToken cancellationToken = default);

    /// <summary>Kanal fiyatını günceller.</summary>
    void Update(ChannelPrice channelPrice);

    /// <summary>Kanal fiyatını siler.</summary>
    void Remove(ChannelPrice channelPrice);
}
