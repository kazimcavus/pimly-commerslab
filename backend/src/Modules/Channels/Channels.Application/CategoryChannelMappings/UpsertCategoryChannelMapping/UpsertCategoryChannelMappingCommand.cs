namespace Channels.Application.CategoryChannelMappings.UpsertCategoryChannelMapping;

/// <summary>
/// Catalog kategorisi ile pazaryeri harici kategorisi arasında kanal eşlemesi oluşturma veya
/// güncelleme komutu.
/// </summary>
/// <param name="MarketplaceCode">Eşlemenin tanımlandığı pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Catalog modülündeki hedef kategori kimliği.</param>
/// <param name="ExternalId">Pazaryerindeki yaprak harici kategori tanımlayıcısı.</param>
public sealed record UpsertCategoryChannelMappingCommand(
    string MarketplaceCode,
    Guid CatalogCategoryId,
    string ExternalId);
