namespace Channels.Application.CategoryChannelMappings.ListCategoryChannelMappings;

/// <summary>
/// Kategori kanal eşlemelerini sayfalı olarak listelemek için kullanılan sorgu modeli.
/// </summary>
/// <param name="MarketplaceCode">Listelenecek eşlemelerin ait olduğu pazaryerinin string anahtarı.</param>
/// <param name="CatalogCategoryId">Yalnızca belirli bir Catalog kategorisine ait eşlemeleri filtrelemek için isteğe bağlı kimlik.</param>
/// <param name="Page">Sayfa numarası (1 tabanlı).</param>
/// <param name="PageSize">Sayfa başına kayıt sayısı.</param>
public sealed record ListCategoryChannelMappingsQuery(
    string MarketplaceCode,
    Guid? CatalogCategoryId,
    int Page,
    int PageSize);
