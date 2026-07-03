namespace Channels.Application.Taxonomy.DeleteAttributeChannelMapping;

/// <summary>
/// Attribute/variant kanal eşlemesini silmek için kullanılan komut modeli.
/// </summary>
/// <param name="MarketplaceKey">Eşlemenin ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Eşlemenin bağlı olduğu Catalog kategori kimliği.</param>
/// <param name="MappingId">Silinecek attribute kanal eşlemesinin benzersiz kimliği.</param>
public sealed record DeleteAttributeChannelMappingCommand(
    string MarketplaceKey,
    Guid CatalogCategoryId,
    Guid MappingId);
