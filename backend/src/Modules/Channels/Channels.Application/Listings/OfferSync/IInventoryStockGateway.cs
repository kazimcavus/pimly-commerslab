namespace Channels.Application.Listings.OfferSync;

/// <summary>
/// Inventory modülünden kalem stoklarını okuyan ACL portu. Channels, Inventory tiplerine doğrudan
/// bağımlanmaz; implementasyon composition root'ta kurulur.
/// </summary>
public interface IInventoryStockGateway
{
    /// <summary>Verilen kalemlerin stok miktarlarını toplu okur; kaydı olmayan kalem sonuçta yer almaz.</summary>
    /// <param name="productItemIds">Okunacak kalem kimlikleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kalem kimliğine göre stok miktarları.</returns>
    Task<IReadOnlyDictionary<Guid, int>> GetQuantitiesAsync(
        IReadOnlyCollection<Guid> productItemIds,
        CancellationToken cancellationToken = default);
}
