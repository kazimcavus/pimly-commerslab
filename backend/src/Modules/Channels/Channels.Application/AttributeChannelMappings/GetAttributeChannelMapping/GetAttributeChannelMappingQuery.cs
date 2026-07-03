namespace Channels.Application.AttributeChannelMappings.GetAttributeChannelMapping;

/// <summary>
/// Tek bir attribute/variant kanal eşlemesini sorgulayan istek modeli.
/// </summary>
/// <param name="MarketplaceCode">Eşlemenin ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Eşlemenin bağlı olduğu Catalog kategori kimliği.</param>
/// <param name="MappingId">Sorgulanacak attribute kanal eşlemesinin benzersiz kimliği.</param>
public sealed record GetAttributeChannelMappingQuery(
    string MarketplaceCode,
    Guid CatalogCategoryId,
    Guid MappingId);
