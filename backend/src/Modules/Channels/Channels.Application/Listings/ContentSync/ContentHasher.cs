using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Channels.Application.Listings.ContentSync;

/// <summary>
/// Gönderilecek içeriğin parmak izini üretir. Hash değişmediyse pazaryerine çağrı yapılmaz —
/// içerik gönderimi ürünü yeniden onaya soktuğu için gereksiz çağrıdan kaçınmak kritiktir.
/// </summary>
public static class ContentHasher
{
    /// <summary>
    /// Payload'ın içerik alanlarından kararlı bir SHA-256 hash üretir. Fiyat ve stok
    /// <em>bilinçli olarak dışarıda</em> bırakılır: onlar ucuz teklif ucundan gider ve içerik
    /// gönderimini tetiklememelidir.
    /// </summary>
    /// <param name="listing">Parmak izi çıkarılacak payload.</param>
    /// <returns>64 karakterlik hex hash.</returns>
    public static string Compute(MarketplaceListingRequest listing)
    {
        var builder = new StringBuilder();
        builder.Append(listing.Barcode).Append('|')
            .Append(listing.Title).Append('|')
            .Append(listing.Description).Append('|')
            .Append(listing.ExternalCategoryId).Append('|')
            .Append(listing.BrandExternalId).Append('|')
            .Append(listing.BrandName).Append('|')
            .Append(listing.ModelCode).Append('|')
            .Append(listing.Sku).Append('|');

        // Sıralama kararlı olmalı: aynı içerik farklı sırayla gelirse hash değişmemeli.
        foreach (var attribute in listing.Attributes
            .OrderBy(attribute => attribute.ExternalAttributeId, StringComparer.Ordinal))
        {
            builder.Append(attribute.ExternalAttributeId).Append('=')
                .Append(attribute.ExternalValueId ?? attribute.CustomValue).Append(';');
        }

        builder.Append('|');
        foreach (var imageUrl in listing.ImageUrls)
        {
            builder.Append(imageUrl).Append(';');
        }

        var canonical = builder.ToString();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Kültürden bağımsız ondalık biçimlendirme (hash girdilerinde tutarlılık için).</summary>
    /// <param name="value">Biçimlendirilecek değer.</param>
    /// <returns>Invariant biçimde metin.</returns>
    public static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
