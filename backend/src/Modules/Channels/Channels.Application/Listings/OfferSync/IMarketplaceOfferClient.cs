using Channels.Application.Connections;
using SharedKernel;

namespace Channels.Application.Listings.OfferSync;

/// <summary>
/// Canlı listelemelerin fiyat ve stok bilgisini pazaryerinde güncelleyen istemci.
/// </summary>
/// <remarks>
/// İçerik (başlık, görsel, attribute) gönderiminden <em>kasten</em> ayrıdır: pazaryerlerinde teklif
/// güncellemesi ucuzdur, toplu çalışır ve ürünü yeniden onaya sokmaz. İkisini tek uca yığmak, stok
/// değişiminde ürünün onay kuyruğuna düşmesine ve geçici satış kaybına yol açar.
/// </remarks>
public interface IMarketplaceOfferClient
{
    /// <summary>Gets tek çağrıda gönderilebilecek azami kalem sayısı.</summary>
    int MaxBatchSize { get; }

    /// <summary>Verilen kalemlerin fiyat/stok bilgisini pazaryerinde günceller.</summary>
    /// <param name="marketplace">Hedef pazaryeri.</param>
    /// <param name="credentials">Pazaryeri kimlik bilgileri.</param>
    /// <param name="offers">Gönderilecek teklifler; <see cref="MaxBatchSize"/> ile sınırlıdır.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gönderim sonucu.</returns>
    Task<Result<OfferUpdateReceipt>> UpdateOffersAsync(
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        IReadOnlyList<MarketplaceOfferUpdate> offers,
        CancellationToken cancellationToken = default);
}

/// <summary>Pazaryerine gönderilen tek kalem teklifi.</summary>
/// <param name="ExternalListingId">Kalemin pazaryerindeki kimliği (Trendyol: barkod).</param>
/// <param name="Quantity">Gönderilecek stok miktarı.</param>
/// <param name="Amount">Satış fiyatı.</param>
/// <param name="CompareAtAmount">Opsiyonel üstü çizili fiyat.</param>
/// <param name="Currency">Para birimi (ISO 4217).</param>
public sealed record MarketplaceOfferUpdate(
    string ExternalListingId,
    int Quantity,
    decimal Amount,
    decimal? CompareAtAmount,
    string Currency);

/// <summary>Teklif gönderiminin pazaryeri tarafındaki makbuzu.</summary>
/// <param name="SubmissionReference">Asenkron takip için batch referansı; yoksa null.</param>
public sealed record OfferUpdateReceipt(string? SubmissionReference);

/// <summary>Pazaryeri koduna göre teklif istemcisi çözümleyicisi.</summary>
public interface IMarketplaceOfferClientResolver
{
    /// <summary>Pazaryeri için kayıtlı teklif istemcisini çözer.</summary>
    /// <param name="marketplace">Hedef pazaryeri.</param>
    /// <returns>Çözümlenen istemci veya hata.</returns>
    Result<IMarketplaceOfferClient> Resolve(Marketplace marketplace);
}
