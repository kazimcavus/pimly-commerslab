namespace Channels.Application.ExternalCatalog.SearchExternalCategories;

/// <summary>
/// Cache'lenmiş harici pazaryeri kategorilerinde arama yapmak için kullanılan sorgu modeli.
/// </summary>
/// <param name="MarketplaceCode">Aramanın yapılacağı pazaryerinin string anahtarı.</param>
/// <param name="Query">Kategori adı veya yol üzerinde aranacak metin; boş bırakılırsa tüm sonuçlar dönebilir.</param>
/// <param name="Limit">Döndürülecek maksimum sonuç sayısı (varsayılan 25).</param>
public sealed record SearchExternalCategoriesQuery(string MarketplaceCode, string? Query, int Limit = 25);
