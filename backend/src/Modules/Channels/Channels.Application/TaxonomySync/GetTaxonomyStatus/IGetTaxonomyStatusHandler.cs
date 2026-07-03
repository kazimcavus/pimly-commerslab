using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.TaxonomySync.GetTaxonomyStatus;

/// <summary>
/// <see cref="GetTaxonomyStatusQuery"/> sorgusunu işleyerek pazaryeri taxonomy özet durumu getirme
/// sözleşmesini tanımlar.
/// </summary>
public interface IGetTaxonomyStatusHandler
{
    /// <summary>
    /// Belirtilen pazaryeri için taxonomy senkronizasyon özet durumunu getirir.
    /// </summary>
    /// <param name="query">Hedef pazaryeri anahtarını içeren sorgu.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Özet durum bilgilerini taşıyan <see cref="TaxonomyStatusDto"/> veya hata.</returns>
    Task<Result<TaxonomyStatusDto>> ExecuteAsync(
        GetTaxonomyStatusQuery query,
        CancellationToken cancellationToken = default);
}
