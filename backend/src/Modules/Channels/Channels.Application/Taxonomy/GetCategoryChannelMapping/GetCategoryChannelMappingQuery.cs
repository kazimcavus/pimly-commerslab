namespace Channels.Application.Taxonomy.GetCategoryChannelMapping;

/// <summary>
/// Tek bir kategori kanal eşlemesini sorgulayan istek modeli.
/// </summary>
/// <param name="MarketplaceKey">Eşlemenin ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Sorgulanacak Catalog kategori kimliği.</param>
public sealed record GetCategoryChannelMappingQuery(
    string MarketplaceKey,
    Guid CatalogCategoryId);
