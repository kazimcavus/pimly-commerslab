namespace Channels.Application.Contracts;

/// <summary>Bir ürünün bağlı pazaryerlerine yayın hazırlık özeti.</summary>
public sealed record ProductReadinessDto(
    Guid ProductId,
    IReadOnlyList<ChannelReadinessDto> Channels);

/// <summary>Tek pazaryeri için hazırlık durumu.</summary>
/// <remarks>
/// Ready = kategori eşli + zorunlu özellik eksiği yok + barkodsuz kalem yok.
/// Zorunluluklar pazaryerinin kendi kategori şemasından (external_category_attributes) okunur;
/// PIM'e kopyalanmış zorunluluklar burada rol oynamaz.
/// </remarks>
public sealed record ChannelReadinessDto(
    string MarketplaceCode,
    string MarketplaceName,
    bool CategoryMapped,
    bool Ready,
    int TotalItems,
    int ItemsMissingBarcode,
    IReadOnlyList<MissingChannelAttributeDto> MissingAttributes);

/// <summary>Pazaryerinin zorunlu tuttuğu ama üründe eksik olan özellik.</summary>
/// <remarks>Reason: "unmapped" (PIM özelliğiyle eşlenmemiş) | "unfilled" (eşli ama değer seçilmemiş).</remarks>
public sealed record MissingChannelAttributeDto(
    string ExternalAttributeId,
    string Name,
    string Reason,
    int MissingItemCount);
