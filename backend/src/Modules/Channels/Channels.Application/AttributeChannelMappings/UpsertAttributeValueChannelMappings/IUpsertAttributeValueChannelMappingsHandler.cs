using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.AttributeChannelMappings.UpsertAttributeValueChannelMappings;

/// <summary>
/// <see cref="UpsertAttributeValueChannelMappingsCommand"/> komutunu işleyerek attribute değer kanal
/// eşlemelerini toplu oluşturma/güncelleme sözleşmesini tanımlar.
/// </summary>
public interface IUpsertAttributeValueChannelMappingsHandler
{
    /// <summary>
    /// Bir attribute kanal eşlemesi altında Catalog değerleri ile harici değerler arasında toplu eşleme yapar.
    /// </summary>
    /// <param name="command">Pazaryeri, kategori, üst eşleme kimliği ve değer girdilerini içeren komut.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Oluşturulan veya güncellenen <see cref="AttributeValueChannelMappingDto"/> listesi veya hata.</returns>
    Task<Result<IReadOnlyList<AttributeValueChannelMappingDto>>> ExecuteAsync(
        UpsertAttributeValueChannelMappingsCommand command,
        CancellationToken cancellationToken = default);
}
