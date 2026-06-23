using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using Catalog.Domain.SkuGenerator;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.SkuGenerator.UpdateSkuGeneratorConfig;

/// <summary>SKU oluşturucu yapılandırmasını güncelleme işlemini yürütür.</summary>
public sealed class UpdateSkuGeneratorConfigHandler(
    IValidator<UpdateSkuGeneratorConfigCommand> validator,
    ISkuGeneratorConfigRepository configRepository,
    IUnitOfWork unitOfWork) : IUpdateSkuGeneratorConfigHandler
{
    /// <inheritdoc/>
    public async Task<Result<SkuGeneratorConfigDto>> ExecuteAsync(
        UpdateSkuGeneratorConfigCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<SkuGeneratorConfigDto>(validationResult.Error);
        }

        var config = await configRepository.GetAsync(cancellationToken);
        if (config is null)
        {
            return Result.Failure<SkuGeneratorConfigDto>(Error.NotFound("SKU generator is not configured."));
        }

        var segments = command.Segments.Select(segment => segment.ToDomain()).ToList();
        var updateResult = config.UpdateSettings(command.Enabled, segments, command.CounterNextValue);
        if (updateResult.IsFailure)
        {
            return Result.Failure<SkuGeneratorConfigDto>(updateResult.Error);
        }

        configRepository.Update(config);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(config.ToDto());
    }
}
