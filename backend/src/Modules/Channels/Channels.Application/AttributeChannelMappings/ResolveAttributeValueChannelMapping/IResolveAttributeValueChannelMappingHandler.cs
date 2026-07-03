using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.AttributeChannelMappings.ResolveAttributeValueChannelMapping;

/// <summary>
/// <see cref="ResolveAttributeValueChannelMappingQuery"/> sorgusunu işleyerek Catalog değerinden harici
/// value id çözümleme sözleşmesini tanımlar.
/// </summary>
public interface IResolveAttributeValueChannelMappingHandler
{
    /// <summary>
    /// Catalog attribute veya variant değeri için tanımlı kanal eşlemesinden harici value id'sini çözümler.
    /// </summary>
    /// <param name="query">Üst attribute eşlemesi kimliği ve Catalog değer kimliğini içeren sorgu.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Çözümlenen harici değer bilgisini taşıyan <see cref="ResolvedAttributeValueChannelMappingDto"/> veya hata.</returns>
    Task<Result<ResolvedAttributeValueChannelMappingDto>> ExecuteAsync(
        ResolveAttributeValueChannelMappingQuery query,
        CancellationToken cancellationToken = default);
}
