using SharedKernel;

namespace Channels.Domain.Listings;

/// <summary>
/// <see cref="ProductListing"/> aggregate'lerinin kalıcılık arabirimi. Sorgular ambient tenant
/// bağlamında çalışır; yalnızca <see cref="ListDirtyScopesAsync"/> tenant sınırını aşar (senkron
/// worker'ı hangi tenant/pazaryeri çiftinde iş olduğunu tenant bağlamı olmadan keşfeder).
/// </summary>
public interface IProductListingRepository
{
    /// <summary>Bir kalemin belirtilen pazaryerindeki listelemesini getirir.</summary>
    Task<ProductListing?> GetAsync(
        Marketplace marketplace,
        Guid productItemId,
        CancellationToken cancellationToken = default);

    /// <summary>Bir kalemin tüm pazaryerlerindeki listelemelerini getirir (kirlilik işaretlemesi için).</summary>
    Task<IReadOnlyList<ProductListing>> ListByProductItemAsync(
        Guid productItemId,
        CancellationToken cancellationToken = default);

    /// <summary>Verilen kalemlerin belirtilen pazaryerindeki listelemelerini toplu getirir (seed için).</summary>
    Task<IReadOnlyList<ProductListing>> ListByProductItemsAsync(
        Marketplace marketplace,
        IReadOnlyCollection<Guid> productItemIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gönderilmeyi bekleyen (kirli ve backoff penceresi dolmuş) listelemeleri en eski kirlilikten
    /// başlayarak getirir.
    /// </summary>
    Task<IReadOnlyList<ProductListing>> ListDirtyAsync(
        Marketplace marketplace,
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bekleyen işi olan (tenant, pazaryeri) çiftlerini tenant bağlamı olmadan keşfeder; senkron
    /// worker'ının pompası bu liste üzerinden döner.
    /// </summary>
    Task<IReadOnlyList<ListingSyncScope>> ListDirtyScopesAsync(
        IReadOnlyCollection<Guid>? tenantIds,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>Yeni listeleme kaydı ekler.</summary>
    Task AddAsync(ProductListing listing, CancellationToken cancellationToken = default);

    /// <summary>Birden çok listeleme kaydını ekler (import seed'i).</summary>
    Task AddRangeAsync(IReadOnlyCollection<ProductListing> listings, CancellationToken cancellationToken = default);

    /// <summary>Listeleme üzerindeki değişiklikleri kalıcı hale getirmek için işaretler.</summary>
    void Update(ProductListing listing);
}

/// <summary>Senkron worker'ının işleyeceği tenant + pazaryeri çifti.</summary>
public sealed record ListingSyncScope(Guid TenantId, Marketplace Marketplace);
