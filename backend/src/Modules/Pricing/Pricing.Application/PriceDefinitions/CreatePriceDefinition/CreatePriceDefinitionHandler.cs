using FluentValidation;
using Pricing.Application.Contracts;
using Pricing.Application.Validation;
using Pricing.Domain;
using Pricing.Domain.PriceDefinitions;
using SharedKernel;

namespace Pricing.Application.PriceDefinitions.CreatePriceDefinition;

/// <summary>Yeni fiyat tanımı oluşturma işlemini yürüten handler.</summary>
public sealed class CreatePriceDefinitionHandler(
    IValidator<CreatePriceDefinitionCommand> validator,
    IPriceDefinitionRepository priceDefinitions,
    IUnitOfWork unitOfWork) : ICreatePriceDefinitionHandler
{
    /// <inheritdoc/>
    public async Task<Result<PriceDefinitionDto>> ExecuteAsync(
        CreatePriceDefinitionCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<PriceDefinitionDto>(validationResult.Error);
        }

        if (await priceDefinitions.GetByNameAsync(command.Name, cancellationToken) is not null)
        {
            return Result.Failure<PriceDefinitionDto>(
                Error.Conflict("Price definition with the same name already exists."));
        }

        var createResult = PriceDefinition.Create(command.Name, command.Code);
        if (createResult.IsFailure)
        {
            return Result.Failure<PriceDefinitionDto>(createResult.Error);
        }

        await priceDefinitions.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(createResult.Value.ToDto());
    }
}
