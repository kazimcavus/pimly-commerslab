using SharedKernel;

namespace Catalog.Domain.Products.Events;

/// <summary>
/// Bir satılabilir kalem kaldırıldığında (kalem silme veya ürünün tümüyle silinmesi) yayımlanan integration olayı.
/// Uydu context'ler ilgili fiyat/stok kayıtlarını temizlemek için dinler.
/// </summary>
/// <example>ProductItemId ve bağlı ProductId ile yayımlanır.</example>
public sealed record ProductItemDeleted(Guid ProductItemId, Guid ProductId) : IntegrationEvent;
