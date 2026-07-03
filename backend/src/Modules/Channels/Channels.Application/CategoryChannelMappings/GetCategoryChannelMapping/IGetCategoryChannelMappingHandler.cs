using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.CategoryChannelMappings.GetCategoryChannelMapping;

/// <summary>
/// <see cref="GetCategoryChannelMappingQuery"/> sorgusunu işleyerek tek kategori kanal eşlemesi getirme
/// sözleşmesini tanımlar.
/// </summary>
public interface IGetCategoryChannelMappingHandler
{
    /// <summary>
    /// Belirtilen Catalog kategorisi için tanımlı kategori kanal eşlemesini getirir.
    /// </summary>
    /// <param name="query">Pazaryeri ve Catalog kategori kimliğini içeren sorgu.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Zenginleştirilmiş <see cref="CategoryChannelMappingDto"/> veya hata.</returns>
    Task<Result<CategoryChannelMappingDto>> ExecuteAsync(
        GetCategoryChannelMappingQuery query,
        CancellationToken cancellationToken = default);
}
