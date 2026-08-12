using SharedKernel;

namespace Catalog.Domain.Products.Events;

/// <summary>
/// Ürünün pazaryerine giden içeriği (başlık, açıklama, marka, kategori, özellik, görsel, barkod/SKU)
/// değiştiğinde yayımlanan integration olayı. Channels bu sinyalle ilgili listelemeleri
/// "içerik kirli" işaretler.
/// </summary>
/// <remarks>
/// <para>Etkilenen kalemler olayla birlikte taşınır: listeleme kaydı kalem düzeyindedir, ürün
/// düzeyinde bir değişiklik (ör. başlık) ürünün tüm kalemlerini etkiler.</para>
/// <para>Olay değer taşımaz; gönderim anında güncel içerik Catalog'dan okunur. Fiyat/stok değişimi
/// bu olayı <em>tetiklemez</em> — onlar ucuz teklif ucundan gider ve yeniden onaya sokmaz.</para>
/// </remarks>
/// <example>ProductId ve etkilenen kalem kimlikleriyle yayımlanır.</example>
public sealed record ProductContentChanged(Guid ProductId, IReadOnlyList<Guid> ProductItemIds) : IntegrationEvent;
