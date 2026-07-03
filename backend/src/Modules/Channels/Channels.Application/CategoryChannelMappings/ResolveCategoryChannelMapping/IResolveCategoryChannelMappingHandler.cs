using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.CategoryChannelMappings.ResolveCategoryChannelMapping;

/// <summary>
/// <see cref="ResolveCategoryChannelMappingQuery"/> sorgusunu işleyerek Catalog kategorisinden harici
/// kategori id çözümleme sözleşmesini tanımlar.
/// </summary>
public interface IResolveCategoryChannelMappingHandler
{
    /// <summary>
    /// Catalog kategorisi için tanımlı kategori kanal eşlemesinden harici pazaryeri kategori id'sini çözümler.
    /// </summary>
    /// <param name="query">Pazaryeri ve Catalog kategori kimliğini içeren sorgu.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Çözümlenen harici kategori bilgisini taşıyan <see cref="ResolvedCategoryChannelMappingDto"/> veya hata.</returns>
    Task<Result<ResolvedCategoryChannelMappingDto>> ExecuteAsync(
        ResolveCategoryChannelMappingQuery query,
        CancellationToken cancellationToken = default);
}
