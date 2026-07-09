namespace Pricing.Application.Contracts;

/// <summary>Kalemin bir pazaryerindeki (kanal) fiyatı DTO'su.</summary>
public sealed record ChannelPriceDto(
    Guid ProductItemId,
    string Marketplace,
    decimal Amount,
    decimal? CompareAtAmount,
    string Currency,
    DateTimeOffset UpdatedAt);
