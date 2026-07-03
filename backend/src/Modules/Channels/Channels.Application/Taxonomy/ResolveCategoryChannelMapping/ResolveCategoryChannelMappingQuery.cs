namespace Channels.Application.Taxonomy.ResolveCategoryChannelMapping;

/// <summary>
/// Catalog kategorisi için harici pazaryeri kategori kimliğini çözümlemek amacıyla kullanılan dahili sorgu modeli.
/// </summary>
/// <param name="MarketplaceKey">Eşlemenin aranacağı pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Harici id'si çözümlenecek Catalog kategori kimliği.</param>
public sealed record ResolveCategoryChannelMappingQuery(
    string MarketplaceKey,
    Guid CatalogCategoryId);
