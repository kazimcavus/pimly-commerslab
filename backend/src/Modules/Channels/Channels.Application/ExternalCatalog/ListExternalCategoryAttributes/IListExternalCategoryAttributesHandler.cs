using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.ExternalCatalog.ListExternalCategoryAttributes;

/// <summary>
/// <see cref="ListExternalCategoryAttributesQuery"/> sorgusunu işleyerek harici kategori attribute'larını
/// listeleme sözleşmesini tanımlar.
/// </summary>
public interface IListExternalCategoryAttributesHandler
{
    /// <summary>
    /// Eşlenmiş Catalog kategorisi için pazaryeri attribute'larını çeker, cache'ler ve listeler.
    /// </summary>
    /// <param name="query">Pazaryeri ve Catalog kategori kimliğini içeren sorgu.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Harici kategori attribute listesini taşıyan <see cref="ExternalCategoryAttributeDto"/> koleksiyonu veya hata.</returns>
    Task<Result<IReadOnlyList<ExternalCategoryAttributeDto>>> ExecuteAsync(
        ListExternalCategoryAttributesQuery query,
        CancellationToken cancellationToken = default);
}
