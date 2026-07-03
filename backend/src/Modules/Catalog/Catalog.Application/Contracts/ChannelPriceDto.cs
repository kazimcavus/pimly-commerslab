namespace Catalog.Application.Contracts;

/// <summary>
/// Kalemin pazaryeri (kanal) bazlı fiyat DTO'su.
/// Etkin satış fiyatı kuralı: kanal fiyatı varsa o, yoksa kalemin temel fiyatı.
/// </summary>
public sealed record ChannelPriceDto(
    Guid Id,
    Guid ProductItemId,
    string MarketplaceKey,
    decimal Price,
    decimal? CompareAtPrice,
    string Currency,
    DateTimeOffset UpdatedAt);
