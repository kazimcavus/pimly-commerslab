using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.AttributeChannelMappings.UpsertAttributeChannelMapping;

/// <summary>
/// <see cref="UpsertAttributeChannelMappingCommand"/> komutunu işleyerek attribute/variant kanal eşlemesi
/// oluşturma/güncelleme sözleşmesini tanımlar.
/// </summary>
public interface IUpsertAttributeChannelMappingHandler
{
    /// <summary>
    /// Catalog attribute veya variant ile harici pazaryeri attribute alanı arasında eşleme oluşturur veya günceller.
    /// </summary>
    /// <param name="command">Pazaryeri, kategori, kaynak tipi, Catalog kaynak ve harici attribute bilgilerini içeren komut.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Oluşturulan veya güncellenen <see cref="AttributeChannelMappingDto"/> veya hata.</returns>
    Task<Result<AttributeChannelMappingDto>> ExecuteAsync(
        UpsertAttributeChannelMappingCommand command,
        CancellationToken cancellationToken = default);
}
