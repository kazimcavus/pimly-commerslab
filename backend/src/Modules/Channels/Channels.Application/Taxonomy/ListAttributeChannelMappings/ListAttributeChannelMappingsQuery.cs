namespace Channels.Application.Taxonomy.ListAttributeChannelMappings;

/// <summary>
/// Attribute/variant kanal eşlemelerini sayfalı olarak listelemek için kullanılan sorgu modeli.
/// </summary>
/// <param name="MarketplaceKey">Listelenecek eşlemelerin ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Eşlemelerin filtreleneceği Catalog kategori kimliği.</param>
/// <param name="SourceType">Yalnızca belirli kaynak tipindeki eşlemeleri filtrelemek için isteğe bağlı değer (<c>CatalogAttribute</c> veya <c>CatalogVariant</c>).</param>
/// <param name="Page">Sayfa numarası (1 tabanlı).</param>
/// <param name="PageSize">Sayfa başına kayıt sayısı.</param>
public sealed record ListAttributeChannelMappingsQuery(
    string MarketplaceKey,
    Guid CatalogCategoryId,
    string? SourceType,
    int Page,
    int PageSize);
