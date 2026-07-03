namespace Channels.Application.Taxonomy.UpsertAttributeValueChannelMappings;

/// <summary>
/// Tek bir Catalog değeri ile harici pazaryeri değeri arasındaki eşleme girdisi.
/// </summary>
/// <param name="CatalogValueId">Catalog attribute veya variant değer kimliği.</param>
/// <param name="ExternalValueId">Pazaryerindeki hedef harici değer tanımlayıcısı.</param>
public sealed record AttributeValueChannelMappingEntry(
    Guid CatalogValueId,
    string ExternalValueId);

/// <summary>
/// Bir attribute/variant kanal eşlemesi altında değer eşlemelerini toplu olarak oluşturma veya
/// güncelleme komutu.
/// </summary>
/// <param name="MarketplaceKey">Eşlemenin ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Üst alan eşlemesinin bağlı olduğu Catalog kategori kimliği.</param>
/// <param name="MappingId">Değer eşlemelerinin bağlı olduğu üst attribute kanal eşlemesi kimliği.</param>
/// <param name="Values">Oluşturulacak veya güncellenecek Catalog-harici değer eşleme girdileri.</param>
public sealed record UpsertAttributeValueChannelMappingsCommand(
    string MarketplaceKey,
    Guid CatalogCategoryId,
    Guid MappingId,
    IReadOnlyList<AttributeValueChannelMappingEntry> Values);
