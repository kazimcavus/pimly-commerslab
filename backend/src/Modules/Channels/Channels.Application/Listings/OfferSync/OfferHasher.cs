using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Channels.Application.Listings.OfferSync;

/// <summary>
/// Gönderilecek teklifin parmak izini üretir. Hash değişmediyse pazaryerine hiç çağrı yapılmaz —
/// 5.000 kalemlik katalogda 3 kalem değiştiyse 3 kalem gider.
/// </summary>
public static class OfferHasher
{
    /// <summary>Teklifin alanlarından kültürden bağımsız, kararlı bir SHA-256 hash üretir.</summary>
    /// <param name="offer">Parmak izi çıkarılacak teklif.</param>
    /// <returns>64 karakterlik hex hash.</returns>
    public static string Compute(MarketplaceOfferUpdate offer)
    {
        // InvariantCulture zorunlu: ondalık ayıracı makineye göre değişirse aynı teklif farklı hash
        // üretir ve her turda gereksiz gönderim olur.
        var canonical = string.Join(
            '|',
            offer.ExternalListingId,
            offer.Quantity.ToString(CultureInfo.InvariantCulture),
            offer.Amount.ToString(CultureInfo.InvariantCulture),
            offer.CompareAtAmount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            offer.Currency);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
