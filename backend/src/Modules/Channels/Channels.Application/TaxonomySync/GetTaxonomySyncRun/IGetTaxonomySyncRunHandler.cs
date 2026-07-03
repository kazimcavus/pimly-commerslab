using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.TaxonomySync.GetTaxonomySyncRun;

/// <summary>
/// <see cref="GetTaxonomySyncRunQuery"/> sorgusunu işleyerek tek bir taxonomy sync run durumu getirme
/// sözleşmesini tanımlar.
/// </summary>
public interface IGetTaxonomySyncRunHandler
{
    /// <summary>
    /// Belirtilen sync run kimliği için ayrıntılı durum bilgisini getirir.
    /// </summary>
    /// <param name="query">Pazaryeri anahtarı ve sync run kimliğini içeren sorgu.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Sync run ayrıntılarını taşıyan <see cref="TaxonomySyncRunDto"/> veya hata.</returns>
    Task<Result<TaxonomySyncRunDto>> ExecuteAsync(
        GetTaxonomySyncRunQuery query,
        CancellationToken cancellationToken = default);
}
