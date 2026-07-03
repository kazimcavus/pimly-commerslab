namespace Channels.Application.Taxonomy.DeleteAttributeValueChannelMapping;

/// <summary>
/// Attribute değer kanal eşlemesini silmek için kullanılan komut modeli.
/// </summary>
/// <param name="MarketplaceKey">Eşlemenin ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Üst alan eşlemesinin bağlı olduğu Catalog kategori kimliği.</param>
/// <param name="MappingId">Değer eşlemesinin bağlı olduğu üst attribute kanal eşlemesi kimliği.</param>
/// <param name="ValueMappingId">Silinecek değer eşlemesinin benzersiz kimliği.</param>
public sealed record DeleteAttributeValueChannelMappingCommand(
    string MarketplaceKey,
    Guid CatalogCategoryId,
    Guid MappingId,
    Guid ValueMappingId);
