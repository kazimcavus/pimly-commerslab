using SharedKernel;

namespace Channels.Domain.Listings;

/// <summary>
/// Bir satılabilir kalemin bir pazaryerindeki kalıcı listeleme durumu. Kanonik <see cref="ProductItemId"/>
/// ile pazaryerindeki <see cref="ExternalListingId"/> arasındaki köprüdür; ikinci gönderimin "güncelle"
/// olmasını (duplicate ürün kartı açılmamasını) ve yalnız değişenin push edilmesini sağlar.
/// </summary>
/// <remarks>
/// <para><b>İçerik ve teklif ayrımı:</b> Pazaryerlerinde fiyat/stok güncellemesi ucuzdur ve yeniden onaya
/// girmez; içerik güncellemesi ise pahalıdır ve ürünü onay kuyruğuna geri düşürür. Bu yüzden iki ayrı
/// hash ve iki ayrı kirlilik damgası tutulur — stok değişimi asla içerik güncellemesi tetiklemez.</para>
/// <para><b>Doğal anahtar:</b> (<see cref="TenantId"/>, <see cref="Marketplace"/>,
/// <see cref="ProductItemId"/>) — unique index ile korunur.</para>
/// <para><b>Kapsam dışı:</b> Payload'ın kendisini (yalnız hash'ini), fiyat/stok değerlerini ve
/// kategori/attribute eşlemesini saklamaz; bunlar Catalog, Pricing, Inventory ve
/// <see cref="Channels.Domain.CategoryChannelMappings.CategoryChannelMapping"/> sahipliğindedir.</para>
/// </remarks>
public sealed class ProductListing : AggregateRoot<Guid>
{
    /// <summary>Dış listeleme kimliği için azami uzunluk.</summary>
    public const int ExternalListingIdMaxLength = 200;

    /// <summary>Gönderim referansı (ör. Trendyol batchRequestId) için azami uzunluk.</summary>
    public const int SubmissionReferenceMaxLength = 200;

    /// <summary>Payload hash'i için azami uzunluk (SHA-256 hex).</summary>
    public const int HashMaxLength = 64;

    /// <summary>Red gerekçesi için azami uzunluk.</summary>
    public const int RejectionReasonMaxLength = 1000;

    private ProductListing()
    {
        Marketplace = null!;
    }

    private ProductListing(
        Guid id,
        Guid tenantId,
        Marketplace marketplace,
        Guid productItemId,
        ListingStatus status,
        string? externalListingId,
        DateTimeOffset dirtyAt)
        : base(id)
    {
        TenantId = tenantId;
        Marketplace = marketplace;
        ProductItemId = productItemId;
        Status = status;
        ExternalListingId = externalListingId;

        // Hash'ler bilinmediği için listing baştan kirlidir: ilk senkron turunda kanonik veri
        // pazaryerine uzlaştırılır.
        ContentDirtyAt = dirtyAt;
        OfferDirtyAt = dirtyAt;
    }

    /// <summary>Gets listeleme sahibi tenant kimliği.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets listelemenin yapıldığı pazaryeri.</summary>
    public Marketplace Marketplace { get; private set; }

    /// <summary>Gets listelenen kanonik satılabilir kalemin kimliği.</summary>
    public Guid ProductItemId { get; private set; }

    /// <summary>Gets listeleme durumu.</summary>
    public ListingStatus Status { get; private set; }

    /// <summary>Gets pazaryerindeki listeleme kimliği; onay gelene kadar null olabilir.</summary>
    public string? ExternalListingId { get; private set; }

    /// <summary>Gets son gönderimin pazaryeri referansı (Trendyol: batchRequestId).</summary>
    public string? SubmissionReference { get; private set; }

    /// <summary>Gets son gönderilen içerik payload'ının hash'i; delta gönderimde çağrıyı atlamak için.</summary>
    public string? ContentHash { get; private set; }

    /// <summary>Gets son gönderilen fiyat/stok payload'ının hash'i.</summary>
    public string? OfferHash { get; private set; }

    /// <summary>Gets içeriğin değiştiğinin işaretlendiği an; null ise içerik güncel sayılır.</summary>
    public DateTimeOffset? ContentDirtyAt { get; private set; }

    /// <summary>Gets fiyat/stok değiştiğinin işaretlendiği an; null ise teklif güncel sayılır.</summary>
    public DateTimeOffset? OfferDirtyAt { get; private set; }

    /// <summary>Gets pazaryerine son gönderim zamanı.</summary>
    public DateTimeOffset? LastSubmittedAt { get; private set; }

    /// <summary>Gets pazaryerinin son onay/sonuç bildirimi zamanı.</summary>
    public DateTimeOffset? LastConfirmedAt { get; private set; }

    /// <summary>Gets pazaryerinin red gerekçesi; yalnızca <see cref="ListingStatus.Rejected"/> durumunda.</summary>
    public string? RejectionReason { get; private set; }

    /// <summary>Gets ardışık başarısız senkron denemesi sayısı; başarıda sıfırlanır.</summary>
    public int SyncAttempts { get; private set; }

    /// <summary>Gets bir sonraki denemenin en erken zamanı (exponential backoff); null ise beklemez.</summary>
    public DateTimeOffset? NextAttemptAt { get; private set; }

    /// <summary>Gets a value indicating whether gönderilmeyi bekleyen bir değişiklik var mı.</summary>
    public bool IsDirty => ContentDirtyAt is not null || OfferDirtyAt is not null;

    /// <summary>
    /// Yayınlanmak üzere yeni listeleme kaydı açar. Henüz gönderilmediği için
    /// <see cref="ListingStatus.Pending"/> durumunda ve kirli başlar.
    /// </summary>
    public static Result<ProductListing> Create(
        Guid tenantId,
        Marketplace marketplace,
        Guid productItemId,
        DateTimeOffset createdAt)
    {
        var validation = Validate(tenantId, marketplace, productItemId);
        if (validation.IsFailure)
        {
            return Result.Failure<ProductListing>(validation.Error);
        }

        return Result.Success(new ProductListing(
            Guid.NewGuid(),
            tenantId,
            marketplace,
            productItemId,
            ListingStatus.Pending,
            externalListingId: null,
            createdAt));
    }

    /// <summary>
    /// Pazaryerinde zaten var olan bir listelemeyi kaydeder (import ile keşfedilen kalemler).
    /// <see cref="ListingStatus.Live"/> başlar; hash'ler bilinmediği için ilk senkron turunda uzlaştırılır.
    /// </summary>
    public static Result<ProductListing> Seed(
        Guid tenantId,
        Marketplace marketplace,
        Guid productItemId,
        string externalListingId,
        DateTimeOffset discoveredAt)
    {
        var validation = Validate(tenantId, marketplace, productItemId);
        if (validation.IsFailure)
        {
            return Result.Failure<ProductListing>(validation.Error);
        }

        var normalizedId = NormalizeExternalListingId(externalListingId);
        if (normalizedId is null)
        {
            return Result.Failure<ProductListing>(Error.Validation("External listing id is required."));
        }

        var listing = new ProductListing(
            Guid.NewGuid(),
            tenantId,
            marketplace,
            productItemId,
            ListingStatus.Live,
            normalizedId,
            discoveredAt)
        {
            LastConfirmedAt = discoveredAt,
        };

        return Result.Success(listing);
    }

    /// <summary>İçeriğin değiştiğini işaretler; idempotenttir, ilk damga korunur.</summary>
    public void MarkContentDirty(DateTimeOffset at) => ContentDirtyAt ??= at;

    /// <summary>Fiyat veya stoğun değiştiğini işaretler; idempotenttir, ilk damga korunur.</summary>
    public void MarkOfferDirty(DateTimeOffset at) => OfferDirtyAt ??= at;

    /// <summary>
    /// Verilen içerik hash'i için gönderim gerekli mi. Hash aynıysa pazaryerine hiç çağrı yapılmaz.
    /// Kaldırma sürecindeki listeler içerik güncellemesi almaz.
    /// </summary>
    public bool NeedsContentSync(string contentHash) =>
        Status is not (ListingStatus.PendingDelist or ListingStatus.Delisted)
        && !string.Equals(ContentHash, contentHash, StringComparison.Ordinal);

    /// <summary>
    /// Verilen fiyat/stok hash'i için gönderim gerekli mi. Teklif güncellemesi yalnızca pazaryerinde
    /// karşılığı olan (dış kimliği bilinen) listelemeler için anlamlıdır.
    /// </summary>
    public bool NeedsOfferSync(string offerHash) =>
        ExternalListingId is not null
        && Status is not (ListingStatus.PendingDelist or ListingStatus.Delisted)
        && !string.Equals(OfferHash, offerHash, StringComparison.Ordinal);

    /// <summary>Backoff penceresi dolmuş mu; başarısız denemeler arasında beklemeyi zorlar.</summary>
    public bool IsSyncDue(DateTimeOffset now) => NextAttemptAt is null || NextAttemptAt <= now;

    /// <summary>
    /// İçerik gönderimini kaydeder: hash saklanır, kirlilik temizlenir ve listeleme onay bekler duruma geçer.
    /// </summary>
    public Result MarkContentSubmitted(string contentHash, string? submissionReference, DateTimeOffset at)
    {
        if (Status is ListingStatus.PendingDelist or ListingStatus.Delisted)
        {
            return Result.Failure(Error.Conflict("Delisted listings cannot be submitted."));
        }

        var normalizedHash = NormalizeHash(contentHash);
        if (normalizedHash is null)
        {
            return Result.Failure(Error.Validation("Content hash is required."));
        }

        ContentHash = normalizedHash;
        ContentDirtyAt = null;
        SubmissionReference = Truncate(submissionReference, SubmissionReferenceMaxLength);
        LastSubmittedAt = at;
        Status = ListingStatus.Submitted;
        RejectionReason = null;
        ResetBackoff();
        return Result.Success();
    }

    /// <summary>
    /// Fiyat/stok gönderimini kaydeder. Durumu <em>değiştirmez</em>: teklif güncellemesi pazaryerinde
    /// yeniden onay tetiklemez, dolayısıyla canlı listeleme canlı kalır.
    /// </summary>
    public Result MarkOfferSynced(string offerHash, DateTimeOffset at)
    {
        if (ExternalListingId is null)
        {
            return Result.Failure(Error.Conflict("Offer sync requires a published listing."));
        }

        var normalizedHash = NormalizeHash(offerHash);
        if (normalizedHash is null)
        {
            return Result.Failure(Error.Validation("Offer hash is required."));
        }

        OfferHash = normalizedHash;
        OfferDirtyAt = null;
        LastSubmittedAt = at;
        ResetBackoff();
        return Result.Success();
    }

    /// <summary>Pazaryerinin listelemeyi kabul ettiğini kaydeder.</summary>
    public Result MarkLive(string externalListingId, DateTimeOffset at)
    {
        var normalizedId = NormalizeExternalListingId(externalListingId);
        if (normalizedId is null)
        {
            return Result.Failure(Error.Validation("External listing id is required."));
        }

        ExternalListingId = normalizedId;
        Status = ListingStatus.Live;
        LastConfirmedAt = at;
        RejectionReason = null;
        ResetBackoff();
        return Result.Success();
    }

    /// <summary>
    /// Pazaryerinin içeriği reddettiğini kaydeder. Yalnızca <em>içerik</em> reddi içindir; ağ/altyapı
    /// hataları için <see cref="RegisterSyncFailure"/> kullanılır.
    /// </summary>
    public Result MarkRejected(string reason, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(Error.Validation("Rejection reason is required."));
        }

        Status = ListingStatus.Rejected;
        RejectionReason = Truncate(reason, RejectionReasonMaxLength);
        LastConfirmedAt = at;

        // İçerik reddedildiği için saklanan hash artık pazaryerindeki durumu temsil etmiyor;
        // düzeltilmiş içerik aynı hash'e sahip olsa bile yeniden gönderilmeli.
        ContentHash = null;
        ContentDirtyAt = at;
        return Result.Success();
    }

    /// <summary>
    /// Geçici (taşıma/altyapı) senkron hatasını kaydeder: durum korunur, kirlilik temizlenmez, bir
    /// sonraki deneme <paramref name="nextAttemptAt"/> sonrasına ertelenir.
    /// </summary>
    public void RegisterSyncFailure(DateTimeOffset nextAttemptAt)
    {
        SyncAttempts++;
        NextAttemptAt = nextAttemptAt;
    }

    /// <summary>Yayından kaldırma talebini kaydeder.</summary>
    public Result RequestDelist()
    {
        if (Status is ListingStatus.Delisted)
        {
            return Result.Failure(Error.Conflict("Listing is already delisted."));
        }

        Status = ListingStatus.PendingDelist;
        ContentDirtyAt = null;
        OfferDirtyAt = null;
        ResetBackoff();
        return Result.Success();
    }

    /// <summary>Pazaryerinden kaldırıldığını kaydeder.</summary>
    public Result MarkDelisted(DateTimeOffset at)
    {
        if (Status is not ListingStatus.PendingDelist)
        {
            return Result.Failure(Error.Conflict("Only listings pending delist can be marked as delisted."));
        }

        Status = ListingStatus.Delisted;
        LastConfirmedAt = at;
        ResetBackoff();
        return Result.Success();
    }

    private static Result Validate(Guid tenantId, Marketplace marketplace, Guid productItemId)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Tenant id is required."));
        }

        if (marketplace is null)
        {
            return Result.Failure(Error.Validation("Marketplace is required."));
        }

        if (productItemId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Product item id is required."));
        }

        return Result.Success();
    }

    private static string? NormalizeExternalListingId(string externalListingId) =>
        string.IsNullOrWhiteSpace(externalListingId)
            ? null
            : Truncate(externalListingId.Trim(), ExternalListingIdMaxLength);

    private static string? NormalizeHash(string hash) =>
        string.IsNullOrWhiteSpace(hash) ? null : Truncate(hash.Trim(), HashMaxLength);

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private void ResetBackoff()
    {
        SyncAttempts = 0;
        NextAttemptAt = null;
    }
}
