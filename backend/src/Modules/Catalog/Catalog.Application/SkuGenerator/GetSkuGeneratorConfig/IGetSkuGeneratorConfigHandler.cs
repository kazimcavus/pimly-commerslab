using Catalog.Application.Contracts;
using SharedKernel;

namespace Catalog.Application.SkuGenerator.GetSkuGeneratorConfig;

/// <summary>SKU oluşturucu yapılandırmasını getirme handler sözleşmesi.</summary>
public interface IGetSkuGeneratorConfigHandler
{
    Task<Result<SkuGeneratorConfigDto>> ExecuteAsync(
        GetSkuGeneratorConfigQuery query,
        CancellationToken cancellationToken = default);
}
