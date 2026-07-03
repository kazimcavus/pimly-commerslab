namespace Channels.Application.Taxonomy.GetAttributeChannelMapping;

/// <summary>
/// Tek bir attribute/variant kanal eşlemesini sorgulayan istek modeli.
/// </summary>
/// <param name="MarketplaceKey">Eşlemenin ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Eşlemenin bağlı olduğu Catalog kategori kimliği.</param>
/// <param name="MappingId">Sorgulanacak attribute kanal eşlemesinin benzersiz kimliği.</param>
public sealed record GetAttributeChannelMappingQuery(
    string MarketplaceKey,
    Guid CatalogCategoryId,
    Guid MappingId);
