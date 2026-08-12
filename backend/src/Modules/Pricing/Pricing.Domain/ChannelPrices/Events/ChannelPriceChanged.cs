using SharedKernel;

namespace Pricing.Domain.ChannelPrices.Events;

/// <summary>
/// Bir kalemin belirli pazaryerindeki kararlaştırılmış fiyatı değiştiğinde yayımlanan integration olayı.
/// Channels bu sinyalle o pazaryerindeki listelemeyi "teklif kirli" işaretler.
/// </summary>
/// <remarks>
/// Olay tutar taşımaz, yalnızca kimlik taşır: gönderim anında güncel fiyat Pricing'den okunur.
/// <see cref="MarketplaceCode"/> taşınır çünkü kirlilik yalnızca ilgili pazaryerinin listelemesine yazılır.
/// </remarks>
/// <example>ProductItemId ve "TY" ile yayımlanır.</example>
public sealed record ChannelPriceChanged(Guid ProductItemId, string MarketplaceCode) : IntegrationEvent;
