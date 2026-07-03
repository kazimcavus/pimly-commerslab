namespace Channels.Application.ExternalCatalog.ListExternalCategoryAttributes;

/// <summary>
/// Eşlenmiş bir Catalog kategorisi için pazaryeri harici attribute'larını listelemek amacıyla
/// kullanılan sorgu modeli.
/// </summary>
/// <param name="MarketplaceCode">Attribute'ların çekileceği pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Harici attribute'ları listelenecek Catalog kategori kimliği.</param>
public sealed record ListExternalCategoryAttributesQuery(
    string MarketplaceCode,
    Guid CatalogCategoryId);
