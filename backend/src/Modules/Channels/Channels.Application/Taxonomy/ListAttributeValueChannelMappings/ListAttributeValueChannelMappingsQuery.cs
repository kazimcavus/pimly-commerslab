namespace Channels.Application.Taxonomy.ListAttributeValueChannelMappings;

/// <summary>
/// Bir attribute/variant kanal eşlemesi altındaki değer eşlemelerini listelemek için kullanılan sorgu modeli.
/// </summary>
/// <param name="MarketplaceKey">Eşlemenin ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Üst alan eşlemesinin bağlı olduğu Catalog kategori kimliği.</param>
/// <param name="MappingId">Değer eşlemelerinin listeleneceği üst attribute kanal eşlemesi kimliği.</param>
public sealed record ListAttributeValueChannelMappingsQuery(
    string MarketplaceKey,
    Guid CatalogCategoryId,
    Guid MappingId);
