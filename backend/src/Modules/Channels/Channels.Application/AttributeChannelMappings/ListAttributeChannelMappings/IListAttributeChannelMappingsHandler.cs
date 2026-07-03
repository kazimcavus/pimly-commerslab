using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.AttributeChannelMappings.ListAttributeChannelMappings;

/// <summary>
/// <see cref="ListAttributeChannelMappingsQuery"/> sorgusunu işleyerek attribute kanal eşlemelerini
/// sayfalı listeleme sözleşmesini tanımlar.
/// </summary>
public interface IListAttributeChannelMappingsHandler
{
    /// <summary>
    /// Belirtilen Catalog kategorisi için attribute/variant kanal eşlemelerini sayfalı olarak listeler.
    /// </summary>
    /// <param name="query">Pazaryeri, kategori, isteğe bağlı kaynak tipi filtresi ve sayfalama bilgilerini içeren sorgu.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Sayfalanmış <see cref="AttributeChannelMappingDto"/> listesi veya hata.</returns>
    Task<Result<PagedResult<AttributeChannelMappingDto>>> ExecuteAsync(
        ListAttributeChannelMappingsQuery query,
        CancellationToken cancellationToken = default);
}
