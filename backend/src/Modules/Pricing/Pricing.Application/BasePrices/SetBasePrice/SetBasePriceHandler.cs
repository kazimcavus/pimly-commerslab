using FluentValidation;
using Pricing.Application.Contracts;
using Pricing.Application.ItemPrices.Catalog;
using Pricing.Application.Validation;
using Pricing.Domain;
using Pricing.Domain.BasePrices;
using SharedKernel;

namespace Pricing.Application.BasePrices.SetBasePrice;

/// <summary>Kalemin temel fiyatını oluşturan / güncelleyen handler.</summary>
public sealed class SetBasePriceHandler(
    IValidator<SetBasePriceCommand> validator,
    ICatalogProductItemGateway productItems,
    IBasePriceRepository basePrices,
    IUnitOfWork unitOfWork) : ISetBasePriceHandler
{
    /// <inheritdoc/>
    public async Task<Result<BasePriceDto>> ExecuteAsync(
        SetBasePriceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateToResultAsync(command, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<BasePriceDto>(validationResult.Error);
        }

        if (!await productItems.ExistsAsync(command.ProductItemId, cancellationToken))
        {
            return Result.Failure<BasePriceDto>(Error.NotFound("Product item not found."));
        }

        var existing = await basePrices.GetByItemAsync(command.ProductItemId, cancellationToken);
        if (existing is not null)
        {
            var updateResult = existing.Update(command.Amount, command.CompareAtAmount, command.Currency);
            if (updateResult.IsFailure)
            {
                return Result.Failure<BasePriceDto>(updateResult.Error);
            }

            basePrices.Update(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(existing.ToDto());
        }

        var createResult = BasePrice.Create(
            command.ProductItemId,
            command.Amount,
            command.CompareAtAmount,
            command.Currency);

        if (createResult.IsFailure)
        {
            return Result.Failure<BasePriceDto>(createResult.Error);
        }

        await basePrices.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(createResult.Value.ToDto());
    }
}
