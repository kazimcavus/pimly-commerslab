using SharedKernel;

namespace Channels.Application.Taxonomy.ProcessTaxonomySync;

/// <summary>
/// Arka plan worker'ının kuyruktaki taxonomy sync job'larını işlemesi için sözleşmeyi tanımlar.
/// Komut parametresi almaz; sıradaki pending run otomatik claim edilir.
/// </summary>
public interface IProcessTaxonomySyncHandler
{
    /// <summary>
    /// Kuyruktan bir pending taxonomy sync job'ını claim eder, pazaryeri kategorilerini indirip cache'ler.
    /// </summary>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    /// <returns>Job işlendiyse <c>true</c>, kuyrukta iş yoksa <c>false</c>; domain hatası durumunda hata.</returns>
    Task<Result<bool>> ExecuteAsync(CancellationToken cancellationToken = default);
}
