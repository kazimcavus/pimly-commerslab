using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.Taxonomy.SearchExternalCategories;

/// <summary>
/// <see cref="SearchExternalCategoriesQuery"/> sorgusunu işleyerek cache'lenmiş harici kategori arama
/// sözleşmesini tanımlar.
/// </summary>
public interface ISearchExternalCategoriesHandler
{
    /// <summary>
    /// Belirtilen pazaryeri cache'inde metin tabanlı kategori araması yapar.
    /// </summary>
    /// <param name="query">Pazaryeri, arama metni ve sonuç limitini içeren sorgu.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Eşleşen kategorilerin <see cref="ExternalCategoryDto"/> listesi veya hata.</returns>
    Task<Result<IReadOnlyList<ExternalCategoryDto>>> ExecuteAsync(
        SearchExternalCategoriesQuery query,
        CancellationToken cancellationToken = default);
}
