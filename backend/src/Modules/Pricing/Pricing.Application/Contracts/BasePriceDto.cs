namespace Pricing.Application.Contracts;

/// <summary>Kalemin temel (site/genel) fiyatı ve opsiyonel karşılaştırma fiyatı DTO'su.</summary>
public sealed record BasePriceDto(
    Guid ProductItemId,
    decimal Amount,
    decimal? CompareAtAmount,
    string Currency,
    DateTimeOffset UpdatedAt);
