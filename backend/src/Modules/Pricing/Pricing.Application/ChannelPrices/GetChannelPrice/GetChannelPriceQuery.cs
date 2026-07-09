namespace Pricing.Application.ChannelPrices.GetChannelPrice;

/// <summary>Kalemin bir pazaryerindeki kanal fiyatını getirme sorgusu.</summary>
public sealed record GetChannelPriceQuery(Guid ProductItemId, string Marketplace);
