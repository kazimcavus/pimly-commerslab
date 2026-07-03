namespace Channels.Application.Taxonomy.ResolveAttributeChannelMapping;

/// <summary>
/// Catalog attribute veya variant kaynağı için harici pazaryeri attribute kimliğini çözümlemek amacıyla
/// kullanılan dahili sorgu modeli.
/// </summary>
/// <param name="MarketplaceKey">Eşlemenin aranacağı pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Eşlemenin bağlı olduğu Catalog kategori kimliği.</param>
/// <param name="SourceType">Catalog kaynak tipi (<c>CatalogAttribute</c> veya <c>CatalogVariant</c>).</param>
/// <param name="CatalogSourceId">Harici attribute id'si çözümlenecek Catalog attribute veya variant kimliği.</param>
public sealed record ResolveAttributeChannelMappingQuery(
    string MarketplaceKey,
    Guid CatalogCategoryId,
    string SourceType,
    Guid CatalogSourceId);
