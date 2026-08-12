using SharedKernel;

namespace Inventory.Domain.StockLevels.Events;

/// <summary>
/// Bir kalemin stok miktarı değiştiğinde yayımlanan integration olayı. Channels bu sinyalle ilgili
/// listelemeleri "teklif kirli" işaretler.
/// </summary>
/// <remarks>
/// Olay yalnızca <em>kimlik</em> taşır, miktar taşımaz: gönderim anında güncel stok okunur. Böylece
/// olayların sırası bozulsa veya tekrarlansa da pazaryerine giden değer doğru kalır.
/// </remarks>
/// <example>ProductItemId ile yayımlanır.</example>
public sealed record StockLevelChanged(Guid ProductItemId) : IntegrationEvent;
