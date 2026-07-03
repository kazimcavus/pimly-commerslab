using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.Taxonomy.ResolveAttributeChannelMapping;

/// <summary>
/// <see cref="ResolveAttributeChannelMappingQuery"/> sorgusunu işleyerek Catalog kaynağından harici
/// attribute id çözümleme sözleşmesini tanımlar.
/// </summary>
public interface IResolveAttributeChannelMappingHandler
{
    /// <summary>
    /// Catalog attribute veya variant kaynağı için tanımlı kanal eşlemesinden harici attribute id'sini çözümler.
    /// </summary>
    /// <param name="query">Pazaryeri, kategori, kaynak tipi ve Catalog kaynak kimliğini içeren sorgu.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Çözümlenen harici attribute bilgisini taşıyan <see cref="ResolvedAttributeChannelMappingDto"/> veya hata.</returns>
    Task<Result<ResolvedAttributeChannelMappingDto>> ExecuteAsync(
        ResolveAttributeChannelMappingQuery query,
        CancellationToken cancellationToken = default);
}
