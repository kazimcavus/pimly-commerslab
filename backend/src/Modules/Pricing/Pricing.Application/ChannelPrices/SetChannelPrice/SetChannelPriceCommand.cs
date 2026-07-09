namespace Pricing.Application.ChannelPrices.SetChannelPrice;

/// <summary>Kalemin bir pazaryerindeki kanal fiyatını oluşturma / güncelleme komutu.</summary>
public sealed record SetChannelPriceCommand(
    Guid ProductItemId,
    string Marketplace,
    decimal Amount,
    decimal? CompareAtAmount = null,
    string? Currency = null);
