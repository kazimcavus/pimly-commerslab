using Catalog.Application.Contracts;
using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;
using SharedKernel;

namespace Catalog.Application.PriceDefinitions.UpdatePriceDefinition;

/// <summary>Fiyat tanımı güncelleme işlemini yürüten handler.</summary>
public sealed class UpdatePriceDefinitionHandler(
    IValidator<UpdatePriceDefinitionCommand> validator,
    IPriceDefinitionRepository priceDefinitions,
    IUnitOfWork unitOfWork) : IUpdatePriceDefinitionHandler
{
    /// <inheritdoc/>
    public async Task<Result<PriceDefinitionDto>> ExecuteAsync(
        UpdatePriceDefinitionCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<PriceDefinitionDto>(validationResult.Error);
        }

        var definition = await priceDefinitions.GetByIdAsync(command.Id, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<PriceDefinitionDto>(Error.NotFound("Price definition not found."));
        }

        var duplicate = await priceDefinitions.GetByNameAsync(command.Name, cancellationToken);
        if (duplicate is not null && duplicate.Id != definition.Id)
        {
            return Result.Failure<PriceDefinitionDto>(
                Error.Conflict("Price definition with the same name already exists."));
        }

        var renameResult = definition.Rename(command.Name, command.Code);
        if (renameResult.IsFailure)
        {
            return Result.Failure<PriceDefinitionDto>(renameResult.Error);
        }

        priceDefinitions.Update(definition);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(definition.ToDto());
    }
}
