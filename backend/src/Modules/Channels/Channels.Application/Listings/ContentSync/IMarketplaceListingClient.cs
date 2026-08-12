using Channels.Application.Connections;
using SharedKernel;

namespace Channels.Application.Listings.ContentSync;

/// <summary>
/// Ürün kartını pazaryerinde oluşturan veya güncelleyen istemci. Yeni ürün gönderimi ve içerik
/// güncellemesi aynı payload'ı kullanır; ikisi de pazaryerinde onay sürecine girer.
/// </summary>
/// <remarks>
/// Fiyat/stok gönderiminden ayrıdır (<see cref="OfferSync.IMarketplaceOfferClient"/>): bu uç pahalıdır
/// ve ürünü yeniden onaya sokar, o yüzden yalnızca içerik gerçekten değiştiğinde çağrılır.
/// </remarks>
public interface IMarketplaceListingClient
{
    /// <summary>Gets tek çağrıda gönderilebilecek azami kalem sayısı.</summary>
    int MaxBatchSize { get; }

    /// <summary>Kalemleri pazaryerinde oluşturur veya günceller.</summary>
    /// <param name="marketplace">Hedef pazaryeri.</param>
    /// <param name="credentials">Pazaryeri kimlik bilgileri.</param>
    /// <param name="listings">Gönderilecek listelemeler.</param>
    /// <param name="isUpdate">true ise mevcut kartlar güncellenir, false ise yeni kart oluşturulur.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gönderim makbuzu.</returns>
    Task<Result<ListingSubmissionReceipt>> SubmitAsync(
        Marketplace marketplace,
        MarketplaceCredentials credentials,
        IReadOnlyList<MarketplaceListingRequest> listings,
        bool isUpdate,
        CancellationToken cancellationToken = default);
}

/// <summary>Pazaryerine gönderilecek tam listeleme payload'ı.</summary>
public sealed record MarketplaceListingRequest(
    Guid ProductItemId,
    string Barcode,
    string Title,
    string? Description,
    string ExternalCategoryId,
    string? BrandExternalId,
    string? BrandName,
    string ModelCode,
    string? Sku,
    decimal Amount,
    decimal? CompareAtAmount,
    string Currency,
    int Quantity,
    IReadOnlyList<MarketplaceListingAttribute> Attributes,
    IReadOnlyList<string> ImageUrls);

/// <summary>Payload içindeki tek bir pazaryeri özelliği.</summary>
/// <param name="ExternalAttributeId">Pazaryerindeki özellik kimliği.</param>
/// <param name="ExternalValueId">Eşlenmiş değer kimliği; serbest metin gönderiliyorsa null.</param>
/// <param name="CustomValue">Değer eşlemesi yoksa gönderilecek serbest metin.</param>
public sealed record MarketplaceListingAttribute(
    string ExternalAttributeId,
    string? ExternalValueId,
    string? CustomValue);

/// <summary>Gönderimin pazaryeri tarafındaki makbuzu.</summary>
/// <param name="SubmissionReference">Asenkron onay takibi için batch referansı; yoksa null.</param>
public sealed record ListingSubmissionReceipt(string? SubmissionReference);

/// <summary>Pazaryeri koduna göre listeleme istemcisi çözümleyicisi.</summary>
public interface IMarketplaceListingClientResolver
{
    /// <summary>Pazaryeri için kayıtlı listeleme istemcisini çözer.</summary>
    /// <param name="marketplace">Hedef pazaryeri.</param>
    /// <returns>Çözümlenen istemci veya hata.</returns>
    Result<IMarketplaceListingClient> Resolve(Marketplace marketplace);
}
