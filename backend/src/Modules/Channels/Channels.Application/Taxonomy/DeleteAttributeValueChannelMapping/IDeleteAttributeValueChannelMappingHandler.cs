using SharedKernel;

namespace Channels.Application.Taxonomy.DeleteAttributeValueChannelMapping;

/// <summary>
/// <see cref="DeleteAttributeValueChannelMappingCommand"/> komutunu işleyerek attribute değer kanal
/// eşlemesi silme sözleşmesini tanımlar.
/// </summary>
public interface IDeleteAttributeValueChannelMappingHandler
{
    /// <summary>
    /// Belirtilen attribute kanal eşlemesi altındaki tek bir değer eşlemesini siler.
    /// </summary>
    /// <param name="command">Pazaryeri, kategori, üst eşleme ve değer eşlemesi kimliklerini içeren komut.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Başarılı silme durumunda boş <see cref="Result"/> veya hata.</returns>
    Task<Result> ExecuteAsync(
        DeleteAttributeValueChannelMappingCommand command,
        CancellationToken cancellationToken = default);
}
