using SharedKernel;

namespace Channels.Application.Publications;

/// <summary>
/// Pricing modülünden bir pazaryerinin kararlaştırılmış kanal fiyatlarını okuyan ACL portu.
/// Yayın (Channels), fiyatı Pricing'de kararlaştırılmış kalemleri bu port üzerinden alır.
/// </summary>
public interface IPricingChannelPriceGateway
{
    /// <summary>Pazaryerindeki tüm kararlaştırılmış kanal fiyatlarını (tenant kapsamında) listeler.</summary>
    Task<IReadOnlyList<DecidedChannelPrice>> ListForMarketplaceAsync(
        Marketplace marketplace,
        CancellationToken cancellationToken = default);
}

/// <summary>Pricing'de bir kalem için kararlaştırılmış kanal fiyatı anlık görüntüsü.</summary>
public sealed record DecidedChannelPrice(
    Guid ProductItemId,
    decimal Amount,
    decimal? CompareAtAmount,
    string Currency);
