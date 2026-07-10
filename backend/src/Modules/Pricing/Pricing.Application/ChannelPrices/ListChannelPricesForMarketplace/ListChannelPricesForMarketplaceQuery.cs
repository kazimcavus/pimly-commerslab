namespace Pricing.Application.ChannelPrices.ListChannelPricesForMarketplace;

/// <summary>Bir pazaryerindeki tüm kanal fiyatlarını (tenant kapsamında) listeleme sorgusu.</summary>
public sealed record ListChannelPricesForMarketplaceQuery(string Marketplace);
