using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.AttributeChannelMappings.ListAttributeValueChannelMappings;

/// <summary>
/// <see cref="ListAttributeValueChannelMappingsQuery"/> sorgusunu işleyerek attribute değer kanal
/// eşlemelerini listeleme sözleşmesini tanımlar.
/// </summary>
public interface IListAttributeValueChannelMappingsHandler
{
    /// <summary>
    /// Belirtilen attribute kanal eşlemesi altındaki tüm değer eşlemelerini listeler.
    /// </summary>
    /// <param name="query">Pazaryeri, Catalog kategori ve üst eşleme kimliğini içeren sorgu.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Zenginleştirilmiş <see cref="AttributeValueChannelMappingDto"/> listesi veya hata.</returns>
    Task<Result<IReadOnlyList<AttributeValueChannelMappingDto>>> ExecuteAsync(
        ListAttributeValueChannelMappingsQuery query,
        CancellationToken cancellationToken = default);
}
