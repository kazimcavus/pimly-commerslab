using SharedKernel;

namespace Channels.Application.Taxonomy.DeleteAttributeChannelMapping;

/// <summary>
/// <see cref="DeleteAttributeChannelMappingCommand"/> komutunu işleyerek attribute kanal eşlemesi silme
/// sözleşmesini tanımlar.
/// </summary>
public interface IDeleteAttributeChannelMappingHandler
{
    /// <summary>
    /// Belirtilen attribute/variant kanal eşlemesini ve altındaki değer eşlemelerini siler.
    /// </summary>
    /// <param name="command">Pazaryeri, Catalog kategori ve eşleme kimliğini içeren komut.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Başarılı silme durumunda boş <see cref="Result"/> veya hata.</returns>
    Task<Result> ExecuteAsync(
        DeleteAttributeChannelMappingCommand command,
        CancellationToken cancellationToken = default);
}
