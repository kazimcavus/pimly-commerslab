namespace Channels.Application.CategoryChannelMappings.ResolveCategoryChannelMapping;

/// <summary>
/// Catalog kategorisi için harici pazaryeri kategori kimliğini çözümlemek amacıyla kullanılan dahili sorgu modeli.
/// </summary>
/// <param name="MarketplaceCode">Eşlemenin aranacağı pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Harici id'si çözümlenecek Catalog kategori kimliği.</param>
public sealed record ResolveCategoryChannelMappingQuery(
    string MarketplaceCode,
    Guid CatalogCategoryId);
