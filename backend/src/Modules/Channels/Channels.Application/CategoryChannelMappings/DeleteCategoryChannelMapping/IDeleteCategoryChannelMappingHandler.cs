using SharedKernel;

namespace Channels.Application.CategoryChannelMappings.DeleteCategoryChannelMapping;

/// <summary>
/// <see cref="DeleteCategoryChannelMappingCommand"/> komutunu işleyerek kategori kanal eşlemesi silme
/// sözleşmesini tanımlar.
/// </summary>
public interface IDeleteCategoryChannelMappingHandler
{
    /// <summary>
    /// Belirtilen Catalog kategorisi için tanımlı kategori kanal eşlemesini siler.
    /// </summary>
    /// <param name="command">Pazaryeri ve Catalog kategori kimliğini içeren komut.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Başarılı silme durumunda boş <see cref="Result"/> veya hata.</returns>
    Task<Result> ExecuteAsync(
        DeleteCategoryChannelMappingCommand command,
        CancellationToken cancellationToken = default);
}
