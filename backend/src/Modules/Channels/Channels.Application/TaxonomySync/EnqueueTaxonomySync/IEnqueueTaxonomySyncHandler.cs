using Channels.Application.Contracts;
using SharedKernel;

namespace Channels.Application.TaxonomySync.EnqueueTaxonomySync;

/// <summary>
/// <see cref="EnqueueTaxonomySyncCommand"/> komutunu işleyerek taxonomy sync job'ını kuyruğa alma
/// sözleşmesini tanımlar.
/// </summary>
public interface IEnqueueTaxonomySyncHandler
{
    /// <summary>
    /// Verilen pazaryeri için yeni bir taxonomy senkronizasyon çalıştırması oluşturur ve kuyruğa alır.
    /// </summary>
    /// <param name="command">Hedef pazaryeri anahtarını içeren komut.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Oluşturulan sync run bilgilerini taşıyan <see cref="TaxonomySyncRunDto"/> veya hata.</returns>
    Task<Result<TaxonomySyncRunDto>> ExecuteAsync(
        EnqueueTaxonomySyncCommand command,
        CancellationToken cancellationToken = default);
}
