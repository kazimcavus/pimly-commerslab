namespace Channels.Application.Taxonomy.UpsertAttributeChannelMapping;

/// <summary>
/// Catalog attribute veya variant ile pazaryeri harici attribute alanı arasında kanal eşlemesi
/// oluşturma veya güncelleme komutu.
/// </summary>
/// <param name="MarketplaceKey">Eşlemenin tanımlandığı pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Eşlemenin bağlı olduğu Catalog kategori kimliği.</param>
/// <param name="SourceType">Catalog kaynak tipi (<c>CatalogAttribute</c> veya <c>CatalogVariant</c>).</param>
/// <param name="CatalogSourceId">Eşlenecek Catalog attribute veya variant kimliği.</param>
/// <param name="ExternalAttributeId">Pazaryerindeki hedef harici attribute tanımlayıcısı.</param>
public sealed record UpsertAttributeChannelMappingCommand(
    string MarketplaceKey,
    Guid CatalogCategoryId,
    string SourceType,
    Guid CatalogSourceId,
    string ExternalAttributeId);
