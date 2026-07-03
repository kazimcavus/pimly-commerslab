namespace Channels.Application.CategoryChannelMappings.GetCategoryChannelMapping;

/// <summary>
/// Tek bir kategori kanal eşlemesini sorgulayan istek modeli.
/// </summary>
/// <param name="MarketplaceCode">Eşlemenin ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Sorgulanacak Catalog kategori kimliği.</param>
public sealed record GetCategoryChannelMappingQuery(
    string MarketplaceCode,
    Guid CatalogCategoryId);
