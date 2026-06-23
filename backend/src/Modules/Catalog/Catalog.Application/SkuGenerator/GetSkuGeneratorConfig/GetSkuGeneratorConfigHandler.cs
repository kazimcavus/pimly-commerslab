using Catalog.Application.Contracts;
using Catalog.Domain.SkuGenerator;
using SharedKernel;

namespace Catalog.Application.SkuGenerator.GetSkuGeneratorConfig;

/// <summary>SKU oluşturucu yapılandırmasını getirme işlemini yürütür.</summary>
public sealed class GetSkuGeneratorConfigHandler(ISkuGeneratorConfigRepository configRepository)
    : IGetSkuGeneratorConfigHandler
{
    /// <inheritdoc/>
    public async Task<Result<SkuGeneratorConfigDto>> ExecuteAsync(
        GetSkuGeneratorConfigQuery query,
        CancellationToken cancellationToken = default)
    {
        var config = await configRepository.GetAsync(cancellationToken);
        return config is null
            ? Result.Failure<SkuGeneratorConfigDto>(Error.NotFound("SKU generator is not configured."))
            : Result.Success(config.ToDto());
    }
}
