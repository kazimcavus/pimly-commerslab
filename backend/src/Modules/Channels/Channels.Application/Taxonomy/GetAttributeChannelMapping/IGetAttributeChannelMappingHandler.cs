using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.Taxonomy.GetAttributeChannelMapping;

/// <summary>
/// <see cref="GetAttributeChannelMappingQuery"/> sorgusunu işleyerek tek attribute kanal eşlemesi getirme
/// sözleşmesini tanımlar.
/// </summary>
public interface IGetAttributeChannelMappingHandler
{
    /// <summary>
    /// Belirtilen kimlik ile attribute/variant kanal eşlemesini getirir.
    /// </summary>
    /// <param name="query">Pazaryeri, Catalog kategori ve eşleme kimliğini içeren sorgu.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Zenginleştirilmiş <see cref="AttributeChannelMappingDto"/> veya hata.</returns>
    Task<Result<AttributeChannelMappingDto>> ExecuteAsync(
        GetAttributeChannelMappingQuery query,
        CancellationToken cancellationToken = default);
}
