using SharedKernel;

namespace Channels.Application.ProductImports.ProcessProductImport;

/// <summary>Claim edilmiş ürün import run'ını işleyen handler arabirimi.</summary>
public interface IProcessProductImportHandler
{
    /// <summary>Islemi calistirir.</summary>
    /// <param name="runId">Running durumundaki import run kimliği.</param>
    /// <param name="cancellationToken">Iptal belirteci.</param>
    /// <returns>Islem sonucu.</returns>
    Task<Result> ExecuteAsync(Guid runId, CancellationToken cancellationToken = default);
}
