namespace Channels.Application.Taxonomy.ListExternalCategoryAttributes;

/// <summary>
/// Eşlenmiş bir Catalog kategorisi için pazaryeri harici attribute'larını listelemek amacıyla
/// kullanılan sorgu modeli.
/// </summary>
/// <param name="MarketplaceKey">Attribute'ların çekileceği pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Harici attribute'ları listelenecek Catalog kategori kimliği.</param>
public sealed record ListExternalCategoryAttributesQuery(
    string MarketplaceKey,
    Guid CatalogCategoryId);
