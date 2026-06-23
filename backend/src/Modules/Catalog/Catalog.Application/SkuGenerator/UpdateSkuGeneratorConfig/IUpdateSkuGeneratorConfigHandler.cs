using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.SkuGenerator.UpdateSkuGeneratorConfig;

/// <summary>SKU oluşturucu yapılandırmasını güncelleme handler sözleşmesi.</summary>
public interface IUpdateSkuGeneratorConfigHandler
{
    Task<Result<SkuGeneratorConfigDto>> ExecuteAsync(
        UpdateSkuGeneratorConfigCommand command,
        CancellationToken cancellationToken = default);
}
