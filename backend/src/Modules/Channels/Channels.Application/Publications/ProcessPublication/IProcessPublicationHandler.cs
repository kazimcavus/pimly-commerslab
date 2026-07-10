using SharedKernel;

namespace Channels.Application.Publications.ProcessPublication;

/// <summary>Claim edilmiş bir yayın run'ını işleyen handler arabirimi (yalnızca worker kompozisyonunda kayıtlı).</summary>
public interface IProcessPublicationHandler
{
    /// <summary>Verilen (Running durumundaki) yayın run'ını işler.</summary>
    /// <param name="runId">İşlenecek run kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task<Result> ExecuteAsync(Guid runId, CancellationToken cancellationToken = default);
}
