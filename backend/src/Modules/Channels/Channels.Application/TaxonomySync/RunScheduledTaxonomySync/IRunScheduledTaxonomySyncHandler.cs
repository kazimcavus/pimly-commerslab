using SharedKernel;

namespace Channels.Application.TaxonomySync.RunScheduledTaxonomySync;

/// <summary>
/// Zamanlanmış taxonomy sync dilimi kontrolünü yürüten
/// <see cref="RunScheduledTaxonomySyncCommand"/> işleyici sözleşmesini tanımlar.
/// </summary>
public interface IRunScheduledTaxonomySyncHandler
{
    /// <summary>
    /// Geçerli UTC zaman dilimi için eksik taxonomy sync job'larını kuyruğa alır.
    /// </summary>
    /// <param name="command">Tetikleme komutu (parametre taşımaz, yapılandırma dosyasından okunur).</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Başarıyla kuyruğa alınan sync run sayısı veya hata.</returns>
    Task<Result<int>> ExecuteAsync(
        RunScheduledTaxonomySyncCommand command,
        CancellationToken cancellationToken = default);
}
