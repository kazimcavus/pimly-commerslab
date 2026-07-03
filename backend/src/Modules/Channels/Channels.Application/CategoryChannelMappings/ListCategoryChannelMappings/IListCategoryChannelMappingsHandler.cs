using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.CategoryChannelMappings.ListCategoryChannelMappings;

/// <summary>
/// <see cref="ListCategoryChannelMappingsQuery"/> sorgusunu işleyerek kategori kanal eşlemelerini
/// sayfalı listeleme sözleşmesini tanımlar.
/// </summary>
public interface IListCategoryChannelMappingsHandler
{
    /// <summary>
    /// Belirtilen pazaryeri için kategori kanal eşlemelerini sayfalı olarak listeler.
    /// </summary>
    /// <param name="query">Pazaryeri, isteğe bağlı kategori filtresi ve sayfalama bilgilerini içeren sorgu.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Sayfalanmış <see cref="CategoryChannelMappingDto"/> listesi veya hata.</returns>
    Task<Result<PagedResult<CategoryChannelMappingDto>>> ExecuteAsync(
        ListCategoryChannelMappingsQuery query,
        CancellationToken cancellationToken = default);
}
