using SharedKernel;

namespace Catalog.Domain.Products.Events;

/// <summary>
/// Yeni bir satılabilir kalem oluşturulduğunda yayımlanan integration olayı.
/// Pricing ve Inventory gibi uydu context'ler bu kaleme varsayılan fiyat/stok kaydı açmak için dinler.
/// </summary>
/// <example>ProductItemId ve bağlı ProductId ile yayımlanır.</example>
public sealed record ProductItemCreated(Guid ProductItemId, Guid ProductId) : IntegrationEvent;
