using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.Taxonomy.UpsertCategoryChannelMapping;

/// <summary>
/// <see cref="UpsertCategoryChannelMappingCommand"/> komutunu işleyerek kategori kanal eşlemesi
/// oluşturma/güncelleme sözleşmesini tanımlar.
/// </summary>
public interface IUpsertCategoryChannelMappingHandler
{
    /// <summary>
    /// Catalog kategorisi ile harici pazaryeri kategorisi arasında eşleme oluşturur veya günceller.
    /// </summary>
    /// <param name="command">Pazaryeri, Catalog kategori ve harici kategori bilgilerini içeren komut.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Oluşturulan veya güncellenen <see cref="CategoryChannelMappingDto"/> veya hata.</returns>
    Task<Result<CategoryChannelMappingDto>> ExecuteAsync(
        UpsertCategoryChannelMappingCommand command,
        CancellationToken cancellationToken = default);
}
