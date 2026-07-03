using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.SkuGenerator;
using SharedKernel;

namespace Catalog.Application.SkuGenerator.GetSkuGeneratorConfig;

/// <summary>SKU generator yapılandırmasını getirme işlemini yürütür.</summary>
public sealed class GetSkuGeneratorConfigHandler(
    ISkuGeneratorConfigRepository configs,
    IUnitOfWork unitOfWork) : IGetSkuGeneratorConfigHandler
{
    /// <inheritdoc/>
    public async Task<Result<SkuGeneratorConfigDto>> ExecuteAsync(
        GetSkuGeneratorConfigQuery query,
        CancellationToken cancellationToken = default)
    {
        _ = query;

        var config = await configs.GetAsync(cancellationToken);
        if (config is null)
        {
            config = SkuGeneratorConfig.CreateInitial();
            await configs.AddAsync(config, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(config.ToDto());
    }
}
